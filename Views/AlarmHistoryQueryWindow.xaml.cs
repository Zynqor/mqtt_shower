using System.Windows;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlarmHistoryQueryWindow.xaml 的交互逻辑
/// </summary>
public partial class AlarmHistoryQueryWindow : Window
{
    public AlarmHistoryQueryWindow(AlarmHistoryQueryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
