using System.Collections.Generic;

namespace MqttMonitor.Models;

/// <summary>
/// 命令模板（预配置所有参数的简化版）
/// </summary>
public class CommandTemplate
{
    /// <summary>
    /// 命令描述（显示给用户看的，如"查询一天的数据"、"重启设备"）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 实际的命令名称（发送给设备的，如"queryData"、"restart"）
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// 预配置的命令参数（完整的参数对象，直接发送）
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}
