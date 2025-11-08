using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MqttMonitor.Models;
using Newtonsoft.Json;

namespace MqttMonitor.Services;

/// <summary>
/// 告警配置服务
/// </summary>
public class AlarmConfigService
{
    private readonly LogService _logService;
    private readonly string _configFilePath = "alarm_config.json";
    private readonly string _alertSettingsFilePath = "alert_settings.json";

    public AlarmConfigService(LogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 加载告警配置
    /// </summary>
    public List<AlarmConfig> LoadAlarmConfigs()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var configs = JsonConvert.DeserializeObject<List<AlarmConfig>>(json);
                _logService.LogInfo($"已加载 {configs?.Count ?? 0} 个告警配置");
                return configs ?? new List<AlarmConfig>();
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载告警配置失败");
        }

        return new List<AlarmConfig>();
    }

    /// <summary>
    /// 保存告警配置
    /// </summary>
    public void SaveAlarmConfigs(List<AlarmConfig> configs)
    {
        try
        {
            var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
            _logService.LogInfo($"已保存 {configs.Count} 个告警配置");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存告警配置失败");
            throw;
        }
    }

    /// <summary>
    /// 获取指定设备和测点的告警配置
    /// </summary>
    public AlarmConfig? GetAlarmConfig(string deviceId, string metricName, List<AlarmConfig> configs)
    {
        return configs.FirstOrDefault(c => c.DeviceId == deviceId && c.MetricName == metricName);
    }

    /// <summary>
    /// 加载告警提醒设置
    /// </summary>
    public AlertSettings LoadAlertSettings()
    {
        try
        {
            if (File.Exists(_alertSettingsFilePath))
            {
                var json = File.ReadAllText(_alertSettingsFilePath);
                var settings = JsonConvert.DeserializeObject<AlertSettings>(json);
                _logService.LogInfo("已加载告警提醒设置");
                return settings ?? new AlertSettings();
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载告警提醒设置失败");
        }

        return new AlertSettings();
    }

    /// <summary>
    /// 保存告警提醒设置
    /// </summary>
    public void SaveAlertSettings(AlertSettings settings)
    {
        try
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_alertSettingsFilePath, json);
            _logService.LogInfo("已保存告警提醒设置");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存告警提醒设置失败");
            throw;
        }
    }
}
