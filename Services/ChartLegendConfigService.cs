using System.IO;
using System.Text.Json;
using MqttMonitor.Models;

namespace MqttMonitor.Services;

/// <summary>
/// 图例配置服务
/// </summary>
public class ChartLegendConfigService
{
    private readonly string _configFilePath;
    private readonly LogService _logService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChartLegendConfigService(LogService logService)
    {
        _logService = logService;

        // 使用configs目录下的配置文件路径
        _configFilePath = Path.Combine("configs", "chart_legend_config.json");

        // 确保configs目录存在
        Directory.CreateDirectory("configs");

        // JSON序列化选项
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        _logService.LogInfo($"图例配置文件路径: {_configFilePath}");
    }

    /// <summary>
    /// 保存图例配置
    /// </summary>
    public void SaveConfig(List<ChartLegendItem> legendItems)
    {
        try
        {
            var config = new ChartLegendConfigCollection
            {
                Legends = legendItems.Select(item => new ChartLegendConfig
                {
                    DeviceId = item.DeviceId,
                    ParameterName = item.ParameterName,
                    ColorHex = item.ColorHex,
                    Offset = item.Offset
                }).ToList()
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configFilePath, json);

            _logService.LogInfo($"图例配置已保存，共 {config.Legends.Count} 项");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存图例配置失败");
        }
    }

    /// <summary>
    /// 加载图例配置
    /// </summary>
    public Dictionary<string, Dictionary<string, ChartLegendConfig>> LoadConfig()
    {
        var result = new Dictionary<string, Dictionary<string, ChartLegendConfig>>();

        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logService.LogInfo("图例配置文件不存在，使用默认配置");
                return result;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<ChartLegendConfigCollection>(json, _jsonOptions);

            if (config?.Legends != null)
            {
                // 按设备ID和参数名组织配置
                foreach (var legend in config.Legends)
                {
                    if (!result.ContainsKey(legend.DeviceId))
                    {
                        result[legend.DeviceId] = new Dictionary<string, ChartLegendConfig>();
                    }
                    result[legend.DeviceId][legend.ParameterName] = legend;
                }

                _logService.LogInfo($"图例配置已加载，共 {config.Legends.Count} 项");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载图例配置失败");
        }

        return result;
    }

    /// <summary>
    /// 获取指定设备参数的配置
    /// </summary>
    public ChartLegendConfig? GetConfig(string deviceId, string parameterName,
        Dictionary<string, Dictionary<string, ChartLegendConfig>> configMap)
    {
        if (configMap.TryGetValue(deviceId, out var deviceConfigs) &&
            deviceConfigs.TryGetValue(parameterName, out var config))
        {
            return config;
        }
        return null;
    }
}
