using System.Drawing;
using System.Text.RegularExpressions;
using AForge.Video;
using AForge.Video.DirectShow;
using Tesseract;

namespace ThermalCamera;

/// <summary>
/// USB 열화상 카메라로부터 영상을 가져오고, 좌측 상단 고정 길이 실수 온도값을 OCR로 인식하는 클래스
/// </summary>
public class SThermalCamera
{
    private FilterInfoCollection? _videoDevices;
    private VideoCaptureDevice? _captureDevice;
    private Bitmap? _lastFrame;
    private readonly object _frameLock = new();
    private TesseractEngine? _ocrEngine;
    private bool _ocrInitialized;

    /// <summary>캡처된 최신 프레임 (스냅샷용)</summary>
    public Bitmap? LastFrame
    {
        get { lock (_frameLock) { return _lastFrame?.Clone() as Bitmap; } }
    }

    /// <summary>카메라 연결 여부</summary>
    public bool IsConnected => _captureDevice?.IsRunning ?? false;

    /// <summary>tessdata 경로 (OCR 언어 데이터). null이면 실행 파일 기준 "tessdata"</summary>
    public string? TessDataPath { get; set; }

    /// <summary>
    /// 사용 가능한 USB 비디오 장치 목록 반환
    /// </summary>
    public List<string> GetCameraList()
    {
        var list = new List<string>();
        try
        {
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            for (int i = 0; i < _videoDevices.Count; i++)
                list.Add(_videoDevices[i].Name);
        }
        catch { /* ignore */ }
        return list;
    }

    /// <summary>
    /// 지정한 인덱스의 카메라에 연결
    /// </summary>
    /// <param name="deviceIndex">GetCameraList() 순서의 인덱스 (0-based)</param>
    /// <returns>성공 여부</returns>
    public bool Connect(int deviceIndex)
    {
        Disconnect();
        if (_videoDevices == null)
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        if (deviceIndex < 0 || deviceIndex >= _videoDevices.Count)
            return false;
        try
        {
            var moniker = _videoDevices[deviceIndex].MonikerString;
            _captureDevice = new VideoCaptureDevice(moniker);
            _captureDevice.NewFrame += CaptureDevice_NewFrame;
            _captureDevice.Start();
            return true;
        }
        catch
        {
            _captureDevice = null;
            return false;
        }
    }

    /// <summary>
    /// 카메라 연결 해제
    /// </summary>
    public void Disconnect()
    {
        if (_captureDevice == null) return;
        try
        {
            if (_captureDevice.IsRunning)
            {
                _captureDevice.SignalToStop();
                _captureDevice.WaitForStop();
            }
            _captureDevice.NewFrame -= CaptureDevice_NewFrame;
        }
        catch { /* ignore */ }
        _captureDevice = null;
        lock (_frameLock)
        {
            _lastFrame?.Dispose();
            _lastFrame = null;
        }
    }

    private void CaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        try
        {
            var frame = (Bitmap)eventArgs.Frame.Clone();
            lock (_frameLock)
            {
                _lastFrame?.Dispose();
                _lastFrame = frame;
            }
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// 현재 프레임을 가져와서 좌측 상단 온도값을 OCR로 인식
    /// </summary>
    /// <param name="temperature">인식된 온도값 (실패 시 null)</param>
    /// <returns>캡처된 이미지 (없으면 null)</returns>
    public Bitmap? CaptureAndRecognizeTemperature(out double? temperature)
    {
        temperature = null;
        Bitmap? frame;
        lock (_frameLock)
        {
            frame = _lastFrame?.Clone() as Bitmap;
        }
        if (frame == null) return null;

        try
        {
            temperature = RecognizeTemperatureFromImage(frame);
            return frame;
        }
        catch
        {
            return frame;
        }
    }

    /// <summary>
    /// 이미지에서 좌측 상단 고정 길이 실수 온도값 인식 (예: 21.2 °C)
    /// </summary>
    public double? RecognizeTemperatureFromImage(Bitmap image)
    {
        if (image == null) return null;
        EnsureOcrEngine();
        if (_ocrEngine == null) return null;

        // 좌측 상단 영역만 크롭 (전체의 약 25% 너비, 15% 높이)
        int cropW = Math.Max(80, Math.Min(400, image.Width / 4));
        int cropH = Math.Max(40, Math.Min(120, image.Height / 6));
        using var crop = new Bitmap(cropW, cropH);
        using (var g = Graphics.FromImage(crop))
        {
            g.DrawImage(image, 0, 0, new Rectangle(0, 0, cropW, cropH), GraphicsUnit.Pixel);
        }

        try
        {
            using var pix = BitmapToPix(crop);
            using var page = _ocrEngine.Process(pix);
            var text = page.GetText();
            return ParseTemperatureFromText(text);
        }
        catch
        {
            return null;
        }
    }

    private static Pix BitmapToPix(Bitmap bitmap)
    {
        var path = Path.Combine(Path.GetTempPath(), $"thermal_ocr_{Guid.NewGuid():N}.png");
        try
        {
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return Pix.LoadFromFile(path);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// OCR 결과 문자열에서 실수 온도값 추출 (예: "21.2 °C" -> 21.2)
    /// </summary>
    public static double? ParseTemperatureFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        // 고정 길이 실수: 예 21.2, 36.0, 20.9
        var match = Regex.Match(text, @"(\d{1,3}\.\d)\s*(°C|C)?", RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;
        return null;
    }

    private void EnsureOcrEngine()
    {
        if (_ocrInitialized) return;
        _ocrInitialized = true;
        try
        {
            var dataPath = TessDataPath ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
            if (!Directory.Exists(dataPath))
                dataPath = Path.Combine(AppContext.BaseDirectory, "..", "tessdata");
            if (!Directory.Exists(dataPath))
                return;
            _ocrEngine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
            _ocrEngine.SetVariable("tessedit_char_whitelist", "0123456789.°C ");
        }
        catch
        {
            _ocrEngine = null;
        }
    }

    /// <summary>
    /// OCR 엔진 해제 (앱 종료 시 호출 권장)
    /// </summary>
    public void DisposeOcr()
    {
        _ocrEngine?.Dispose();
        _ocrEngine = null;
        _ocrInitialized = false;
    }
}
