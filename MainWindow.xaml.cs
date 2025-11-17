using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MqttMonitor.ViewModels;
using MqttMonitor.Views;

namespace MqttMonitor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel, LogView logView, TableView tableView, ChartView chartView, CommandSenderView commandSenderView, AlarmView alarmView)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // 将视图添加到对应的 TabItem
        ChartTabItem.Content = chartView;
        TableTabItem.Content = tableView;
        CommandSenderTabItem.Content = commandSenderView;
        LogTabItem.Content = logView;
        AlarmTabItem.Content = alarmView;

        // 订阅窗口事件
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    /// <summary>
    /// 窗口加载时恢复窗口状态
    /// </summary>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.RestoreWindowState(this);
    }

    /// <summary>
    /// 窗口关闭时保存窗口状态并释放资源
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.SaveWindowState(this);

        // 释放 ViewModel 资源（包括设备管理服务）
        _viewModel?.Dispose();
    }
}