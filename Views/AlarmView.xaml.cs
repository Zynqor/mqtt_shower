using System.Windows.Controls;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlarmView.xaml 的交互逻辑
/// </summary>
public partial class AlarmView : UserControl
{
    public AlarmView(AlarmViewModel viewModel, AlarmStatisticsView statisticsView)
    {
        InitializeComponent();
        DataContext = viewModel;

        // 将统计面板添加到右侧
        StatisticsPanel.Children.Add(statisticsView);
    }
}
