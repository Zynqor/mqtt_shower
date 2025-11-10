using System;
using System.IO;
using MqttMonitor.Models;
using Newtonsoft.Json;

namespace MqttMonitor.Services;

/// <summary>
/// 图表配置服务
/// </summary>
public class ChartConfigService
{
    private readonly LogService _logService;
    private readonly string _configFilePath = "chart_config.json";

    public ChartConfigService(LogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 加载图表配置
    /// </summary>
    public ChartConfig LoadChartConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonConvert.DeserializeObject<ChartConfig>(json);
                _logService.LogInfo("已加载图表配置");
                return config ?? new ChartConfig();
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载图表配置失败");
        }

        _logService.LogInfo("使用默认图表配置");
        return new ChartConfig();
    }

    /// <summary>
    /// 保存图表配置
    /// </summary>
    public void SaveChartConfig(ChartConfig config)
    {
        try
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
            _logService.LogInfo("已保存图表配置");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存图表配置失败");
        }
    }
}
