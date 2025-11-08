using System.Windows;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlertSettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class AlertSettingsWindow : Window
{
    private readonly AlertSettingsViewModel _viewModel;

    public AlertSettingsWindow(AlertSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 订阅事件
        _viewModel.OnSettingsSaved += OnSettingsSaved;
        _viewModel.OnCancelled += OnCancelled;
    }

    private void OnSettingsSaved()
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelled()
    {
        DialogResult = false;
        Close();
    }
}
