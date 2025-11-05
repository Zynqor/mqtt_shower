using System.Windows;
using MqttMonitor.Models;

namespace MqttMonitor.Views;

/// <summary>
/// ContactUsWindow.xaml 的交互逻辑
/// </summary>
public partial class ContactUsWindow : Window
{
    public ContactUsWindow(MqttSettings settings)
    {
        InitializeComponent();
        DataContext = settings;
    }
}
