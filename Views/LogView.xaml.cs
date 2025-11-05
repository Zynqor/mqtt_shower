using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// LogView.xaml 的交互逻辑
/// </summary>
public partial class LogView : UserControl
{
    private double _fontSize = 11; // 初始字体大小

    public LogView(LogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Subscribe to the PropertyChanged event to auto-scroll when LogsText changes
        if (DataContext is LogViewModel logViewModel)
        {
            logViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        // 添加鼠标滚轮事件处理，支持 Ctrl+滚轮缩放
        LogTextBox.PreviewMouseWheel += LogTextBox_PreviewMouseWheel;
    }

    /// <summary>
    /// 处理 Ctrl+滚轮缩放字体
    /// </summary>
    private void LogTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // 根据滚轮方向调整字体大小
            if (e.Delta > 0)
            {
                _fontSize = Math.Min(_fontSize + 1, 32); // 最大32
            }
            else
            {
                _fontSize = Math.Max(_fontSize - 1, 6); // 最小6
            }

            LogTextBox.FontSize = _fontSize;
            e.Handled = true; // 阻止默认滚动行为
        }
    }

    /// <summary>
    /// 当 ViewModel 属性变化时，自动滚动到底部
    /// </summary>
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 当 LogsText 属性变化时，滚动到底部
        if (e.PropertyName == nameof(LogViewModel.LogsText))
        {
            // 使用 Dispatcher 确保在 UI 更新完成后再滚动
            Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
