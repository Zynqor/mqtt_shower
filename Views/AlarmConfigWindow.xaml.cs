using System.Windows;
using System.Windows.Controls;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlarmConfigWindow.xaml 的交互逻辑
/// </summary>
public partial class AlarmConfigWindow : Window
{
    private readonly AlarmConfigViewModel _viewModel;

    public AlarmConfigWindow(AlarmConfigViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 订阅事件
        _viewModel.OnConfigSaved += OnConfigSaved;
        _viewModel.OnCancelled += OnCancelled;
    }

    private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item)
        {
            // 只有选中的是测点（DeviceMetricItem）时才显示配置
            if (item.DataContext is DeviceMetricItem metric)
            {
                _viewModel.SelectedMetric = metric;
                e.Handled = true; // 阻止事件继续传播
            }
            else
            {
                // 选中的是设备组，清空选择
                _viewModel.SelectedMetric = null;
            }
        }
    }

    private void OnConfigSaved()
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
