using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MqttMonitor.Models;

/// <summary>
/// MQTT 连接设置
/// </summary>
public class MqttSettings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 应用程序标题
    /// </summary>
    public string Title { get; set; } = "Mqtt Monitor";

    /// <summary>
    /// MQTT 服务器地址
    /// </summary>
    public string Server { get; set; } = "localhost";

    /// <summary>
    /// MQTT 服务器端口
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// 用户名（可选）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码（可选）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 客户端ID（可选，留空则自动生成）
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 基础 Topic（用于命令发送和响应）
    /// </summary>
    public string BaseTopic { get; set; } = "iot/devices";

    /// <summary>
    /// 已订阅的 Topics 列表
    /// </summary>
    public ObservableCollection<string> SubscribedTopics { get; set; } = new ObservableCollection<string>();

    /// <summary>
    /// 窗口宽度
    /// </summary>
    public double WindowWidth { get; set; } = 1000;

    /// <summary>
    /// 窗口高度
    /// </summary>
    public double WindowHeight { get; set; } = 600;

    /// <summary>
    /// 窗口左侧位置
    /// </summary>
    public double WindowLeft { get; set; } = double.NaN;

    /// <summary>
    /// 窗口顶部位置
    /// </summary>
    public double WindowTop { get; set; } = double.NaN;

    /// <summary>
    /// 窗口状态（Normal, Maximized, Minimized）
    /// </summary>
    public string WindowState { get; set; } = "Normal";

    private int _maxChartDataPoints = 1000;
    /// <summary>
    /// 图表最大数据点数量
    /// </summary>
    public int MaxChartDataPoints
    {
        get => _maxChartDataPoints;
        set
        {
            if (_maxChartDataPoints != value)
            {
                _maxChartDataPoints = value;
                OnPropertyChanged();
            }
        }
    }

    private int _chartUpdateInterval = 800;
    /// <summary>
    /// 图表更新间隔（毫秒）
    /// </summary>
    public int ChartUpdateInterval
    {
        get => _chartUpdateInterval;
        set
        {
            if (_chartUpdateInterval != value)
            {
                _chartUpdateInterval = value;
                OnPropertyChanged();
            }
        }
    }

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

    /// <summary>
    /// 启用TLS/SSL加密连接
    /// </summary>
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// CA证书文件路径（用于验证服务器）
    /// </summary>
    public string? CaCertificatePath { get; set; }

    /// <summary>
    /// 客户端证书文件路径（双向认证时使用）
    /// </summary>
    public string? ClientCertificatePath { get; set; }

    /// <summary>
    /// 客户端私钥文件路径（双向认证时使用）
    /// </summary>
    public string? ClientKeyPath { get; set; }

    /// <summary>
    /// 忽略证书错误（仅用于测试，不推荐在生产环境使用）
    /// </summary>
    public bool IgnoreCertificateErrors { get; set; } = false;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
