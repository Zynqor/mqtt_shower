namespace MqttMonitor.Services;

/// <summary>
/// 日志服务，用于记录和广播应用程序日志
/// </summary>
public class LogService
{
    /// <summary>
    /// 当有新日志时触发的事件
    /// </summary>
    public event Action<string>? OnLogReceived;

    /// <summary>
    /// 记录日志消息，附加时间戳并触发事件
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Log(string message)
    {
        var timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        OnLogReceived?.Invoke(timestampedMessage);
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
