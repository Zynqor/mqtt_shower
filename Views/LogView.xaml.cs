using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

        // Subscribe to the collection changed event to auto-scroll
        if (DataContext is LogViewModel logViewModel)
        {
            logViewModel.Logs.CollectionChanged += Logs_CollectionChanged;
        }

        // 添加鼠标滚轮事件处理，支持 Ctrl+滚轮缩放
        LogListBox.PreviewMouseWheel += LogListBox_PreviewMouseWheel;
    }

    /// <summary>
    /// 处理 Ctrl+滚轮缩放字体
    /// </summary>
    private void LogListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

            LogListBox.FontSize = _fontSize;
            e.Handled = true; // 阻止默认滚动行为
        }
    }

    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Scroll to the bottom when new items are added
        if (LogListBox.Items.Count > 0)
        {
            // Find the ScrollViewer within the ListBox
            ScrollViewer? scrollViewer = FindScrollViewer(LogListBox);
            if (scrollViewer != null)
            {
                // Scroll to the bottom
                scrollViewer.ScrollToBottom();
            }
        }
    }

    /// <summary>
    /// Helper method to find the ScrollViewer within a control's visual tree.
    /// </summary>
    private ScrollViewer? FindScrollViewer(DependencyObject parent)
    {
        // Confirm parent and children are valid
        if (parent == null)
            return null;

        // Check if the current object is a ScrollViewer
        if (parent is ScrollViewer sv)
            return sv;

        // Recursively search for a ScrollViewer in the children
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindScrollViewer(child);
            if (result != null)
                return result;
        }

        return null;
    }
}
