namespace MqttMonitor.Models;

/// <summary>
/// 上行数据中的单个测点对象
/// </summary>
public class UpstreamPayload
{
    /// <summary>
    /// 测点名称，例如 "温度"、"CPU占用率"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 测量到的具体数值
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// 数值的单位，例如 "C"、"%"、"V"（可选）
    /// </summary>
    public string? Unit { get; set; }
}

/// <summary>
/// 上行时序数据包
/// </summary>
public class UpstreamDataPacket
{
    /// <summary>
    /// 设备的唯一标识符，例如MAC地址或序列号
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 数据采集时的UTC毫秒级Unix时间戳
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// 包含一个或多个测点对象的数组
    /// </summary>
    public List<UpstreamPayload> Payload { get; set; } = new List<UpstreamPayload>();
}
