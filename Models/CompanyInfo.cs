namespace MqttMonitor.Models;

/// <summary>
/// 公司联系信息配置
/// </summary>
public class CompanyInfo
{
    /// <summary>
    /// 公司名称
    /// </summary>
    public string CompanyName { get; set; } = "您的公司名称";

    /// <summary>
    /// 公司地址
    /// </summary>
    public string CompanyAddress { get; set; } = "您的公司地址";

    /// <summary>
    /// 联系电话
    /// </summary>
    public string CompanyPhone { get; set; } = "联系电话";

    /// <summary>
    /// 公司邮箱
    /// </summary>
    public string CompanyEmail { get; set; } = "contact@company.com";
}
