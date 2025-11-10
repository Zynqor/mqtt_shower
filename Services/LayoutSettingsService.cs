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
    private readonly string _configFilePath = Path.Combine("configs", "layout_settings.json");

    public LayoutSettingsService(LogService logService)
    {
        _logService = logService;
        // 确保configs目录存在
        Directory.CreateDirectory("configs");
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
            // 如果layout_settings.json不存在，尝试从旧的config.json迁移窗口属性
            else if (File.Exists("config.json"))
            {
                try
                {
                    var oldConfigJson = File.ReadAllText("config.json");
                    dynamic? oldConfig = JsonConvert.DeserializeObject(oldConfigJson);
                    if (oldConfig != null)
                    {
                        var settings = new LayoutSettings();
                        if (oldConfig.WindowWidth != null)
                            settings.WindowWidth = (double)oldConfig.WindowWidth;
                        if (oldConfig.WindowHeight != null)
                            settings.WindowHeight = (double)oldConfig.WindowHeight;
                        if (oldConfig.WindowLeft != null)
                            settings.WindowLeft = (double)oldConfig.WindowLeft;
                        if (oldConfig.WindowTop != null)
                            settings.WindowTop = (double)oldConfig.WindowTop;
                        if (oldConfig.WindowState != null)
                            settings.WindowState = (string)oldConfig.WindowState;

                        SaveLayoutSettings(settings);
                        _logService.LogInfo("已从config.json迁移窗口布局到layout_settings.json");
                        return settings;
                    }
                }
                catch (Exception migrationEx)
                {
                    _logService.LogException(migrationEx, "从config.json迁移窗口布局失败");
                }
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
