using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// LogView.xaml 的交互逻辑
/// </summary>
public partial class LogView : UserControl
{
    public LogView(LogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Subscribe to the collection changed event to auto-scroll
        if (DataContext is LogViewModel logViewModel)
        {
            logViewModel.Logs.CollectionChanged += Logs_CollectionChanged;
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
