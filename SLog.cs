using System.Text;

namespace ThermalCamera;

/// <summary>
/// 로그를 Form RichTextBox에 표시하고 파일로 저장하는 클래스
/// </summary>
public class SLog
{
    private RichTextBox? _richTextBox;
    private string _logFilePath;
    private readonly object _lock = new();
    private bool _writeToFile = true;

    public SLog(string? logFilePath = null)
    {
        _logFilePath = logFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ThermalCamera",
            $"log_{DateTime.Now:yyyyMMdd}.txt");
        EnsureLogDirectory();
    }

    /// <summary>
    /// 로그를 출력할 RichTextBox 설정
    /// </summary>
    public void SetRichTextBox(RichTextBox richTextBox)
    {
        _richTextBox = richTextBox;
    }

    /// <summary>
    /// 로그 파일 경로 설정
    /// </summary>
    public void SetLogFilePath(string path)
    {
        _logFilePath = path;
        EnsureLogDirectory();
    }

    /// <summary>
    /// 파일 저장 사용 여부
    /// </summary>
    public bool WriteToFile
    {
        get => _writeToFile;
        set => _writeToFile = value;
    }

    private void EnsureLogDirectory()
    {
        try
        {
            var dir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// 로그 한 줄 추가 (RichTextBox + 파일)
    /// </summary>
    public void WriteLine(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        lock (_lock)
        {
            WriteToRichTextBox(line);
            if (_writeToFile)
                AppendToFile(line);
        }
    }

    /// <summary>
    /// 정보 로그
    /// </summary>
    public void Info(string message) => WriteLine($"[INFO] {message}");

    /// <summary>
    /// 경고 로그
    /// </summary>
    public void Warn(string message) => WriteLine($"[WARN] {message}");

    /// <summary>
    /// 오류 로그
    /// </summary>
    public void Error(string message) => WriteLine($"[ERR] {message}");

    private void WriteToRichTextBox(string line)
    {
        if (_richTextBox == null) return;
        if (_richTextBox.InvokeRequired)
        {
            _richTextBox.Invoke(new Action(() => AppendLineToRichTextBox(line)));
            return;
        }
        AppendLineToRichTextBox(line);
    }

    private void AppendLineToRichTextBox(string line)
    {
        if (_richTextBox == null) return;
        _richTextBox.AppendText(line + Environment.NewLine);
        _richTextBox.ScrollToCaret();
    }

    private void AppendToFile(string line)
    {
        try
        {
            File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // 파일 쓰기 실패 시 무시 (UI 로그는 이미 출력됨)
        }
    }

    /// <summary>
    /// 로그 내용 비우기 (RichTextBox만, 파일은 유지)
    /// </summary>
    public void ClearDisplay()
    {
        if (_richTextBox == null) return;
        if (_richTextBox.InvokeRequired)
        {
            _richTextBox.Invoke(new Action(() => _richTextBox.Clear()));
            return;
        }
        _richTextBox.Clear();
    }
}
