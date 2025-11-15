namespace MqttMonitor.Models;

/// <summary>
/// 设备心跳消息
/// </summary>
public class HeartbeatMessage
{
    /// <summary>
    /// 设备ID（必需）
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 数据Topic（必需）
    /// </summary>
    public string DataTopic { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称（可选）
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 设备类型（可选）
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// 时间戳（可选）
    /// </summary>
    public long? Timestamp { get; set; }
}
