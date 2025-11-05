using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 日志视图 ViewModel
/// </summary>
public class LogViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 日志集合
    /// </summary>
    public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

    public LogViewModel(LogService logService)
    {
        _logService = logService;

        // 订阅日志事件
        _logService.OnLogReceived += OnLogReceived;

        // 添加欢迎日志
        AddLog("应用程序已启动");
    }

    /// <summary>
    /// 当收到新日志时
    /// </summary>
    private void OnLogReceived(string log)
    {
        AddLog(log);
    }

    /// <summary>
    /// 添加日志（确保在 UI 线程上执行）
    /// </summary>
    private void AddLog(string log)
    {
        // 确保在 UI 线程上执行
        if (Application.Current.Dispatcher.CheckAccess())
        {
            Logs.Add(log);

            // 限制日志数量，防止内存无限增长
            if (Logs.Count > 1000)
            {
                Logs.RemoveAt(0);
            }
        }
        else
        {
            Application.Current.Dispatcher.InvokeAsync(() => AddLog(log));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
