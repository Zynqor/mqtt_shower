using System.Windows.Controls;
using MqttMonitor.ViewModels;

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
}
