namespace MqttMonitor.Models;

/// <summary>
/// 布局设置
/// </summary>
public class LayoutSettings
{
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

    /// <summary>
    /// 告警页面左侧面板宽度（相对值）
    /// 默认: 1（对应 1:3 比例）
    /// </summary>
    public double AlarmViewLeftPanelWidth { get; set; } = 1.0;

    /// <summary>
    /// 告警页面右侧面板宽度（相对值）
    /// 默认: 3（对应 1:3 比例）
    /// </summary>
    public double AlarmViewRightPanelWidth { get; set; } = 3.0;

    /// <summary>
    /// 活动告警区域高度（相对值）
    /// 默认: 1（对应 50% 高度）
    /// </summary>
    public double ActiveAlarmHeight { get; set; } = 1.0;

    /// <summary>
    /// 历史告警区域高度（相对值）
    /// 默认: 1（对应 50% 高度）
    /// </summary>
    public double HistoryAlarmHeight { get; set; } = 1.0;
}
