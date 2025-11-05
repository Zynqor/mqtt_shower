using System.Collections.Concurrent;
using System.Text;
using MqttMonitor.Models;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

namespace MqttMonitor.Services;

/// <summary>
/// 数据处理服务，负责解析和处理 MQTT 消息
/// </summary>
public class DataProcessingService
{
    private readonly MqttService _mqttService;
    private readonly LogService _logService;
    private readonly CsvDataStorageService _csvStorageService;

    /// <summary>
    /// 存储所有设备及其指标的最新状态
    /// </summary>
    public ConcurrentDictionary<string, DeviceState> AllDeviceData { get; }
        = new ConcurrentDictionary<string, DeviceState>();

    /// <summary>
    /// 当解析到上行数据时触发
    /// </summary>
    public event Action<UpstreamDataPacket>? OnUpstreamDataParsed;

    /// <summary>
    /// 当解析到命令响应时触发
    /// </summary>
    public event Action<CommandResponse>? OnCommandResponseParsed;

    /// <summary>
    /// 当清空所有数据时触发
    /// </summary>
    public event Action? OnDataCleared;

    public DataProcessingService(MqttService mqttService, LogService logService, CsvDataStorageService csvStorageService)
    {
        _mqttService = mqttService;
        _logService = logService;
        _csvStorageService = csvStorageService;

        // 订阅 MQTT 消息接收事件
        _mqttService.OnMessageReceived += HandleMqttMessage;
    }

    /// <summary>
    /// 处理接收到的 MQTT 消息
    /// </summary>
    private void HandleMqttMessage(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

            _logService.LogInfo($"收到消息 - Topic: {topic}");

            // 判断消息类型
            if (topic.EndsWith("/command/response"))
            {
                // 命令响应
                HandleCommandResponse(payload);
            }
            else
            {
                // 上行数据（默认）
                HandleUpstreamData(payload);
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "处理 MQTT 消息时出错");
        }
    }

    /// <summary>
    /// 处理上行时序数据
    /// </summary>
    private void HandleUpstreamData(string jsonPayload)
    {
        try
        {
            var dataPacket = JsonConvert.DeserializeObject<UpstreamDataPacket>(jsonPayload);
            if (dataPacket == null)
            {
                _logService.LogWarning("上行数据解析失败：数据为空");
                return;
            }

            _logService.LogInfo($"解析上行数据 - 设备ID: {dataPacket.DeviceId}, 测点数量: {dataPacket.Payload?.Count ?? 0}");

            // 更新设备状态
            var deviceState = AllDeviceData.GetOrAdd(dataPacket.DeviceId,
                id => new DeviceState { DeviceId = id });

            // 更新每个测点的最新值
            if (dataPacket.Payload != null)
            {
                foreach (var metric in dataPacket.Payload)
                {
                    deviceState.LatestMetrics.AddOrUpdate(
                        metric.Name,
                        metric,
                        (key, oldValue) => metric);
                }
            }

            // 触发事件
            OnUpstreamDataParsed?.Invoke(dataPacket);

            // 保存数据到CSV文件
            _ = Task.Run(async () =>
            {
                try
                {
                    await _csvStorageService.SaveDataAsync(dataPacket);
                }
                catch (Exception ex)
                {
                    _logService.LogException(ex, "保存CSV数据时出错");
                }
            });
        }
        catch (JsonException ex)
        {
            _logService.LogException(ex, "解析上行数据 JSON 失败");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "处理上行数据时出错");
        }
    }

    /// <summary>
    /// 处理命令响应
    /// </summary>
    private void HandleCommandResponse(string jsonPayload)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<CommandResponse>(jsonPayload);
            if (response == null)
            {
                _logService.LogWarning("命令响应解析失败：数据为空");
                return;
            }

            _logService.LogInfo($"解析命令响应 - CommandId: {response.CommandId}, Status: {response.Status}");

            if (response.Status == "error" && !string.IsNullOrEmpty(response.ErrorMessage))
            {
                _logService.LogError($"命令执行失败 - {response.ErrorMessage}");
            }

            // 触发事件
            OnCommandResponseParsed?.Invoke(response);
        }
        catch (JsonException ex)
        {
            _logService.LogException(ex, "解析命令响应 JSON 失败");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "处理命令响应时出错");
        }
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void ClearAllData()
    {
        AllDeviceData.Clear();
        _logService.LogInfo("已清空所有设备数据");
        OnDataCleared?.Invoke();
    }
}
