using System;
using System.IO;
using MqttMonitor.Models;
using Newtonsoft.Json;

namespace MqttMonitor.Services;

/// <summary>
/// 布局设置服务
/// </summary>
public class LayoutSettingsService
{
    private readonly LogService _logService;
    private readonly string _configFilePath = "layout_settings.json";

    public LayoutSettingsService(LogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 加载布局设置
    /// </summary>
    public LayoutSettings LoadLayoutSettings()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var settings = JsonConvert.DeserializeObject<LayoutSettings>(json);
                _logService.LogInfo("已加载布局设置");
                return settings ?? new LayoutSettings();
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载布局设置失败");
        }

        _logService.LogInfo("使用默认布局设置");
        return new LayoutSettings();
    }

    /// <summary>
    /// 保存布局设置
    /// </summary>
    public void SaveLayoutSettings(LayoutSettings settings)
    {
        try
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
            _logService.LogInfo("已保存布局设置");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存布局设置失败");
        }
    }
}
