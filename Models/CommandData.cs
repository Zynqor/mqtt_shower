using System.Collections.Generic;

namespace MqttMonitor.Models;

/// <summary>
/// 下行命令请求（云端 -> 设备）
/// </summary>
public class CommandRequest
{
    /// <summary>
    /// 命令的唯一标识符，建议使用UUID，用于关联响应
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// 命令的功能名称，例如 "rebootDevice"、"setUploadInterval"
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// 命令下发时的UTC毫秒级Unix时间戳
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// 命令所需的参数（可选）
    /// 可以是字符串、字典或其他JSON可序列化的对象
    /// </summary>
    public object? Params { get; set; }
}

/// <summary>
/// 命令响应（设备 -> 云端）
/// </summary>
public class CommandResponse
{
    /// <summary>
    /// 必须与收到的命令请求中的 CommandId 完全一致
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// 执行状态，必须是 "success" 或 "error" 之一
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 生成此响应时的UTC毫秒级Unix时间戳
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// 当 Status 为 "error" 时，提供详细的错误信息（可选）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 当命令为查询类操作时，在此字段中返回结果（可选）
    /// </summary>
    public Dictionary<string, object>? Result { get; set; }
}
