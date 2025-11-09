namespace MqttMonitor.Models;

/// <summary>
/// 布局设置
/// </summary>
public class LayoutSettings
{
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
