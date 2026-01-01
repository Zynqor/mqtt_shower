using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlarmConfigWindow.xaml 的交互逻辑
/// </summary>
public partial class AlarmConfigWindow : Window
{
    private readonly AlarmConfigViewModel _viewModel;

    // Windows API 常量
    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZEBOX = 0x20000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public AlarmConfigWindow(AlarmConfigViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 禁用最小化按钮
        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_MINIMIZEBOX);
        };

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
