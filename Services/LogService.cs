using System.IO;

namespace MqttMonitor.Services;

/// <summary>
/// 日志服务，用于记录和广播应用程序日志
/// </summary>
public class LogService
{
    private readonly string _logDirectory = "logs";
    private string? _currentLogFile;
    private string? _currentDate;

    /// <summary>
    /// 当有新日志时触发的事件
    /// </summary>
    public event Action<string>? OnLogReceived;

    public LogService()
    {
        // 确保logs目录存在
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    /// <summary>
    /// 记录日志消息，附加时间戳并触发事件
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Log(string message)
    {
        var now = DateTime.Now;
        var timestampedMessage = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        OnLogReceived?.Invoke(timestampedMessage);

        // 写入日志文件
        WriteToFile(timestampedMessage, now);
    }

    /// <summary>
    /// 将日志写入文件
    /// </summary>
    private void WriteToFile(string message, DateTime now)
    {
        try
        {
            var dateString = now.ToString("yyyy-MM-dd");

            // 如果是新的一天，更新日志文件路径
            if (_currentDate != dateString)
            {
                _currentDate = dateString;
                _currentLogFile = Path.Combine(_logDirectory, $"log_{dateString}.txt");
            }

            // 追加日志到文件
            if (_currentLogFile != null)
            {
                File.AppendAllText(_currentLogFile, timestampedMessage + Environment.NewLine);
            }
        }
        catch
        {
            // 忽略文件写入错误，避免影响程序运行
        }
    }

    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    public void LogInfo(string message)
    {
        Log($"[INFO] {message}");
    }

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    public void LogWarning(string message)
    {
        Log($"[WARN] {message}");
    }

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    public void LogError(string message)
    {
        Log($"[ERROR] {message}");
    }

    /// <summary>
    /// 记录异常日志
    /// </summary>
    public void LogException(Exception ex, string? context = null)
    {
        var message = context != null
            ? $"[ERROR] {context}: {ex.Message}\n{ex.StackTrace}"
            : $"[ERROR] {ex.Message}\n{ex.StackTrace}";
        Log(message);
    }
}
