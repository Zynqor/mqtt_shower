using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    // Windows API 常量
    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZEBOX = 0x20000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public SettingsWindow(SettingsViewModel viewModel)
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
        _viewModel.OnSettingsSaved += OnSettingsSaved;
        _viewModel.OnCancelled += OnCancelled;

        // 加载密码
        Loaded += (s, e) =>
        {
            PasswordBox.Password = _viewModel.Password;
        };

        // 同步 PasswordBox 到 ViewModel
        PasswordBox.PasswordChanged += (s, e) =>
        {
            _viewModel.Password = PasswordBox.Password;
        };

        // 监听 IsPasswordVisible 变化，同步两个控件的值
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsPasswordVisible))
            {
                if (_viewModel.IsPasswordVisible)
                {
                    // 切换到显示模式，从 PasswordBox 同步到 TextBox
                    PasswordTextBox.Text = PasswordBox.Password;
                }
                else
                {
                    // 切换到隐藏模式，从 TextBox 同步到 PasswordBox
                    PasswordBox.Password = _viewModel.Password;
                }
            }
        };
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
