using System.Windows;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// DeviceListWindow.xaml 的交互逻辑
/// </summary>
public partial class DeviceListWindow : Window
{
    public DeviceListWindow(DeviceListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
