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

    /// <summary>tessdata 경로 (OCR 언어 데이터). null이면 자동 탐색</summary>
    public string? TessDataPath { get; set; }

    /// <summary>디버그 로그 (tessdata 경로, OCR 원문 등). null이면 로그 없음</summary>
    public Action<string>? OnDebugLog { get; set; }

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
        if (_ocrEngine == null)
        {
            OnDebugLog?.Invoke("OCR 엔진 초기화 실패: tessdata 폴더를 찾을 수 없습니다.");
            return null;
        }

        // 좌측 상단 영역 크롭 (전체의 약 30% 너비, 20% 높이) - 온도 텍스트 영역
        int cropW = Math.Max(120, Math.Min(500, image.Width * 3 / 10));
        int cropH = Math.Max(60, Math.Min(180, image.Height / 5));
        using var crop = new Bitmap(cropW, cropH);
        using (var g = Graphics.FromImage(crop))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, 0, 0, new Rectangle(0, 0, cropW, cropH), GraphicsUnit.Pixel);
        }

        // OCR 정확도 향상: 2배 스케일업 (작은 텍스트 인식 개선)
        const int scale = 2;
        using var scaled = new Bitmap(cropW * scale, cropH * scale);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(crop, 0, 0, scaled.Width, scaled.Height);
        }

        try
        {
            using var pix = BitmapToPix(scaled);
            using var page = _ocrEngine.Process(pix);
            var text = page.GetText()?.Trim() ?? "";
            var temp = ParseTemperatureFromText(text);
            if (!temp.HasValue && !string.IsNullOrEmpty(text))
                OnDebugLog?.Invoke($"OCR 원문: \"{text}\" → 온도 파싱 실패");
            return temp;
        }
        catch (Exception ex)
        {
            OnDebugLog?.Invoke($"OCR 오류: {ex.Message}");
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
        // 고정 길이 실수: 21.2 °C, 36.0, 21,2 등 다양한 형식
        var patterns = new[] { @"(\d{1,3}[.,]\d)", @"(\d{1,3}\.\d)\s*", @"(\d{1,3})[.,](\d)" };
        foreach (var pat in patterns)
        {
            var m = Regex.Match(text, pat);
            if (!m.Success) continue;
            var num = m.Groups.Count > 2
                ? $"{m.Groups[1].Value}.{m.Groups[2].Value}"
                : m.Groups[1].Value.Replace(',', '.');
            if (double.TryParse(num, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var value) && value is > -50 and < 200)
                return value;
        }
        // OCR이 소수점 누락한 경우: "212" -> 21.2, "360" -> 36.0
        var intMatch = Regex.Match(text, @"(\d{2,3})\b");
        if (intMatch.Success && int.TryParse(intMatch.Groups[1].Value, out var intVal))
        {
            var withDecimal = intVal / 10.0;
            if (withDecimal is >= 5 and <= 60) return withDecimal; // 일반 열화상 범위
        }
        return null;
    }

    private void EnsureOcrEngine()
    {
        if (_ocrInitialized) return;
        _ocrInitialized = true;
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            TessDataPath,
            Path.Combine(baseDir, "tessdata"),
            Path.Combine(baseDir, "..", "tessdata"),
            Path.Combine(baseDir, "..", "..", "tessdata"),
            Path.Combine(baseDir, "..", "..", "..", "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
            Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? "", "tessdata"),
        }.Where(p => !string.IsNullOrEmpty(p)).Distinct();

        string? dataPath = null;
        foreach (var p in candidates)
        {
            if (p != null && Directory.Exists(p) && File.Exists(Path.Combine(p, "eng.traineddata")))
            {
                dataPath = Path.GetFullPath(p);
                break;
            }
        }

        if (dataPath == null)
        {
            var searched = string.Join(", ", candidates.Where(p => p != null));
            OnDebugLog?.Invoke($"tessdata 미발견. eng.traineddata가 있는 tessdata 폴더가 필요합니다. 탐색 경로: {searched}");
            return;
        }

        try
        {
            _ocrEngine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
            _ocrEngine.SetVariable("tessedit_char_whitelist", "0123456789. °Cc"); // °C 지원
            OnDebugLog?.Invoke($"OCR 초기화됨: {dataPath}");
        }
        catch (Exception ex)
        {
            OnDebugLog?.Invoke($"Tesseract 초기화 오류: {ex.Message}");
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
