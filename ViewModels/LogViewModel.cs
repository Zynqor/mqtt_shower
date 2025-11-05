using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 日志视图 ViewModel
/// </summary>
public class LogViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private string _logsText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 日志集合
    /// </summary>
    public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

    /// <summary>
    /// 日志文本（用于TextBox显示）
    /// </summary>
    public string LogsText
    {
        get => _logsText;
        private set
        {
            if (_logsText != value)
            {
                _logsText = value;
                OnPropertyChanged();
            }
        }
    }

    public LogViewModel(LogService logService)
    {
        _logService = logService;

        // 订阅日志集合变化事件
        Logs.CollectionChanged += Logs_CollectionChanged;

        // 订阅日志事件
        _logService.OnLogReceived += OnLogReceived;

        // 添加欢迎日志
        AddLog("应用程序已启动");
    }

    /// <summary>
    /// 当Logs集合变化时更新LogsText
    /// </summary>
    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateLogsText();
    }

    /// <summary>
    /// 更新日志文本
    /// </summary>
    private void UpdateLogsText()
    {
        var sb = new StringBuilder();
        foreach (var log in Logs)
        {
            sb.AppendLine(log);
        }
        LogsText = sb.ToString();
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
