using System.Windows.Controls;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// AlarmView.xaml 的交互逻辑
/// </summary>
public partial class AlarmView : UserControl
{
    public AlarmView(AlarmViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
