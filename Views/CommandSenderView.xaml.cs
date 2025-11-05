using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MqttMonitor.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MqttMonitor.Views;

/// <summary>
/// CommandSenderView.xaml 的交互逻辑
/// </summary>
public partial class CommandSenderView : UserControl
{
    public CommandSenderView(CommandSenderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// 历史记录双击事件 - 显示详细信息
    /// </summary>
    private void CommandHistory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CommandHistoryItem historyItem)
        {
            ShowCommandDetails(historyItem);
        }
    }

    /// <summary>
    /// 显示命令详细信息
    /// </summary>
    private void ShowCommandDetails(CommandHistoryItem item)
    {
        var details = new System.Text.StringBuilder();
        details.AppendLine($"时间: {item.Time:yyyy-MM-dd HH:mm:ss}");
        details.AppendLine($"设备ID: {item.DeviceId}");
        details.AppendLine($"命令名称: {item.CommandName}");
        details.AppendLine($"命令ID: {item.CommandId}");
        details.AppendLine($"状态: {item.Status}");

        if (item.ResponseTime.HasValue)
        {
            details.AppendLine($"响应时间: {item.ResponseTime.Value:yyyy-MM-dd HH:mm:ss}");
        }

        if (!string.IsNullOrWhiteSpace(item.Result))
        {
            details.AppendLine();
            details.AppendLine("返回结果:");
            details.AppendLine("─────────────────────────────");

            // 尝试格式化JSON
            try
            {
                var jsonObj = JToken.Parse(item.Result);
                var formattedJson = jsonObj.ToString(Formatting.Indented);
                details.AppendLine(formattedJson);
            }
            catch
            {
                // 如果不是有效的JSON，直接显示原始文本
                details.AppendLine(item.Result);
            }
        }

        // 创建详情窗口
        var detailWindow = new Window
        {
            Title = "命令详情",
            Width = 600,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            Content = new ScrollViewer
            {
                Content = new TextBox
                {
                    Text = details.ToString(),
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New"),
                    FontSize = 13,
                    Padding = new Thickness(10),
                    BorderThickness = new Thickness(0)
                }
            }
        };

        detailWindow.ShowDialog();
    }
}
