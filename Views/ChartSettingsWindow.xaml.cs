using System.Windows;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// ChartSettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class ChartSettingsWindow : Window
{
    public ChartSettingsWindow(ChartSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // 订阅保存和取消事件
        viewModel.OnSettingsSaved += () => this.DialogResult = true;
        viewModel.OnCancelled += () => this.DialogResult = false;
    }
}
