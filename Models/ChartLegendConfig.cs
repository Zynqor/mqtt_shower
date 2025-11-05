namespace MqttMonitor.Models;

/// <summary>
/// 图例配置（用于序列化到JSON）
/// </summary>
public class ChartLegendConfig
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 参数名称
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// 颜色（十六进制格式）
    /// </summary>
    public string ColorHex { get; set; } = string.Empty;

    /// <summary>
    /// 偏移量
    /// </summary>
    public double Offset { get; set; }
}

/// <summary>
/// 图例配置集合
/// </summary>
public class ChartLegendConfigCollection
{
    /// <summary>
    /// 所有图例配置
    /// </summary>
    public List<ChartLegendConfig> Legends { get; set; } = new();
}
