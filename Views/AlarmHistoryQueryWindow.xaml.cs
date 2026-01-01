using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlarmHistoryQueryWindow.xaml 的交互逻辑
/// </summary>
public partial class AlarmHistoryQueryWindow : Window
{
    // Windows API 常量
    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZEBOX = 0x20000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public AlarmHistoryQueryWindow(AlarmHistoryQueryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // 禁用最小化按钮
        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_MINIMIZEBOX);
        };
    }
}
