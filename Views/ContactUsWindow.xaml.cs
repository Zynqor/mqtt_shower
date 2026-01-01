using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MqttMonitor.Models;

namespace MqttMonitor.Views;

/// <summary>
/// ContactUsWindow.xaml 的交互逻辑
/// </summary>
public partial class ContactUsWindow : Window
{
    // Windows API 常量
    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZEBOX = 0x20000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public ContactUsWindow(CompanyInfo companyInfo)
    {
        InitializeComponent();
        DataContext = companyInfo;

        // 禁用最小化按钮
        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_MINIMIZEBOX);
        };
    }
}
