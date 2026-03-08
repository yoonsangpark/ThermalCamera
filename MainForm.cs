namespace ThermalCamera;

public partial class MainForm : Form
{
    private readonly SThermalCamera _camera = new SThermalCamera();
    private readonly SLog _log = new SLog();

    public MainForm()
    {
        InitializeComponent();
        _log.SetRichTextBox(richTextBoxLog);
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        RefreshCameraList();
        _log.Info("프로그램 시작. 카메라를 선택 후 Connect 하세요.");
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _camera.Disconnect();
        _camera.DisposeOcr();
    }

    private void RefreshCameraList()
    {
        listBoxCameras.Items.Clear();
        var list = _camera.GetCameraList();
        foreach (var name in list)
            listBoxCameras.Items.Add(name);
        if (list.Count == 0)
            _log.Warn("연결된 USB 비디오 장치가 없습니다.");
    }

    private void btnConnect_Click(object? sender, EventArgs e)
    {
        int idx = listBoxCameras.SelectedIndex;
        if (idx < 0)
        {
            _log.Warn("카메라를 목록에서 선택하세요.");
            return;
        }
        if (_camera.Connect(idx))
        {
            _log.Info($"연결됨: {listBoxCameras.Items[idx]}");
        }
        else
        {
            _log.Error("카메라 연결 실패.");
        }
    }

    private void btnDisconnect_Click(object? sender, EventArgs e)
    {
        _camera.Disconnect();
        _log.Info("카메라 연결 해제됨.");
    }

    private void btnCaptureAndRecognize_Click(object? sender, EventArgs e)
    {
        if (!_camera.IsConnected)
        {
            _log.Warn("먼저 카메라를 연결하세요.");
            return;
        }

        var bmp = _camera.CaptureAndRecognizeTemperature(out var temperature);

        if (bmp == null)
        {
            _log.Warn("프레임을 가져올 수 없습니다. 잠시 후 다시 시도하세요.");
            return;
        }

        try
        {
            // 캡처 이미지 표시
            var old = pictureBoxPreview.Image;
            pictureBoxPreview.Image = (Bitmap)bmp.Clone();
            old?.Dispose();

            // 온도 표시
            if (temperature.HasValue)
            {
                textBoxTemperature.Text = $"{temperature.Value:F1} °C";
                _log.Info($"온도 인식: {temperature.Value:F1} °C");
            }
            else
            {
                textBoxTemperature.Text = "—";
                _log.Warn("온도값 인식 실패. tessdata(OCR) 경로를 확인하세요.");
            }
        }
        finally
        {
            bmp.Dispose();
        }
    }
}
