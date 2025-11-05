using System.Collections.Concurrent;

namespace MqttMonitor.Models;

/// <summary>
/// 设备状态，包含设备的所有最新测点数据
/// </summary>
public class DeviceState
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 最新的测点数据，Key 为测点名称，Value 为测点数据
    /// </summary>
    public ConcurrentDictionary<string, UpstreamPayload> LatestMetrics { get; set; }
        = new ConcurrentDictionary<string, UpstreamPayload>();
}
