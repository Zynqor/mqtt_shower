using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MqttMonitor.Services;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlarmView.xaml 的交互逻辑
/// </summary>
public partial class AlarmView : UserControl
{
    private readonly LayoutSettingsService _layoutSettingsService;

    public AlarmView(AlarmViewModel viewModel, AlarmStatisticsView statisticsView, LayoutSettingsService layoutSettingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _layoutSettingsService = layoutSettingsService;

        // 将统计面板添加到右侧
        StatisticsPanel.Children.Add(statisticsView);

        // 加载并应用布局设置
        LoadLayoutSettings();

        // 订阅GridSplitter拖动完成事件
        HorizontalSplitter.DragCompleted += OnSplitterDragCompleted;
        VerticalSplitter.DragCompleted += OnSplitterDragCompleted;
    }

    /// <summary>
    /// 加载布局设置
    /// </summary>
    private void LoadLayoutSettings()
    {
        var settings = _layoutSettingsService.LoadLayoutSettings();

        // 应用列宽度
        LeftPanelColumn.Width = new GridLength(settings.AlarmViewLeftPanelWidth, GridUnitType.Star);
        RightPanelColumn.Width = new GridLength(settings.AlarmViewRightPanelWidth, GridUnitType.Star);

        // 应用行高度
        ActiveAlarmRow.Height = new GridLength(settings.ActiveAlarmHeight, GridUnitType.Star);
        HistoryAlarmRow.Height = new GridLength(settings.HistoryAlarmHeight, GridUnitType.Star);
    }

    /// <summary>
    /// GridSplitter拖动完成事件处理
    /// </summary>
    private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        SaveLayoutSettings();
    }

    /// <summary>
    /// 保存当前布局设置
    /// </summary>
    private void SaveLayoutSettings()
    {
        var settings = new Models.LayoutSettings
        {
            AlarmViewLeftPanelWidth = LeftPanelColumn.Width.Value,
            AlarmViewRightPanelWidth = RightPanelColumn.Width.Value,
            ActiveAlarmHeight = ActiveAlarmRow.Height.Value,
            HistoryAlarmHeight = HistoryAlarmRow.Height.Value
        };

        _layoutSettingsService.SaveLayoutSettings(settings);
    }
}
