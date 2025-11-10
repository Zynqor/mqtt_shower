using System;
using System.IO;
using MqttMonitor.Models;
using Newtonsoft.Json;

namespace MqttMonitor.Services;

/// <summary>
/// 公司信息配置服务
/// </summary>
public class CompanyInfoService
{
    private readonly LogService _logService;
    private readonly string _configFilePath = Path.Combine("configs", "company_info.json");

    public CompanyInfoService(LogService logService)
    {
        _logService = logService;
        // 确保configs目录存在
        Directory.CreateDirectory("configs");
    }

    /// <summary>
    /// 加载公司信息
    /// </summary>
    public CompanyInfo LoadCompanyInfo()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var info = JsonConvert.DeserializeObject<CompanyInfo>(json);
                _logService.LogInfo("已加载公司信息配置");
                return info ?? new CompanyInfo();
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载公司信息配置失败");
        }

        _logService.LogInfo("使用默认公司信息");
        return new CompanyInfo();
    }

    /// <summary>
    /// 保存公司信息
    /// </summary>
    public void SaveCompanyInfo(CompanyInfo info)
    {
        try
        {
            var json = JsonConvert.SerializeObject(info, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
            _logService.LogInfo("已保存公司信息配置");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存公司信息配置失败");
        }
    }
}
