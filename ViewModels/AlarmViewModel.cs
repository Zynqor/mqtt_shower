using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 告警页面 ViewModel
/// </summary>
public class AlarmViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private readonly AlarmDetectionService _alarmDetectionService;
    private int _activeAlarmCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 活动告警列表
    /// </summary>
    public ObservableCollection<AlarmRecord> ActiveAlarms => _alarmDetectionService.ActiveAlarms;

    /// <summary>
    /// 历史告警列表
    /// </summary>
    public ObservableCollection<AlarmRecord> HistoryAlarms => _alarmDetectionService.HistoryAlarms;

    /// <summary>
    /// 活动告警数量
    /// </summary>
    public int ActiveAlarmCount
    {
        get => _activeAlarmCount;
        set
        {
            if (_activeAlarmCount != value)
            {
                _activeAlarmCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActiveAlarms));
            }
        }
    }

    /// <summary>
    /// 是否有活动告警
    /// </summary>
    public bool HasActiveAlarms => ActiveAlarmCount > 0;

    // 命令
    public ICommand AcknowledgeCommand { get; }
    public ICommand AcknowledgeAllCommand { get; }
    public ICommand ClearHistoryCommand { get; }

    public AlarmViewModel(LogService logService, AlarmDetectionService alarmDetectionService)
    {
        _logService = logService;
        _alarmDetectionService = alarmDetectionService;

        // 初始化命令
        AcknowledgeCommand = new RelayCommand<AlarmRecord>(OnAcknowledge);
        AcknowledgeAllCommand = new RelayCommand(OnAcknowledgeAll);
        ClearHistoryCommand = new RelayCommand(OnClearHistory);

        // 订阅集合变化
        ActiveAlarms.CollectionChanged += (s, e) =>
        {
            ActiveAlarmCount = ActiveAlarms.Count;
        };

        // 订阅告警事件
        _alarmDetectionService.OnAlarmTriggered += OnAlarmTriggered;
        _alarmDetectionService.OnAlarmRecovered += OnAlarmRecovered;

        // 初始化计数
        ActiveAlarmCount = ActiveAlarms.Count;
    }

    /// <summary>
    /// 告警触发时
    /// </summary>
    private void OnAlarmTriggered(AlarmRecord record)
    {
        // 可以在这里添加额外的UI处理
        _logService.LogInfo($"UI: 告警触发 - {record.Description}");
    }

    /// <summary>
    /// 告警恢复时
    /// </summary>
    private void OnAlarmRecovered(AlarmRecord record)
    {
        // 可以在这里添加额外的UI处理
        _logService.LogInfo($"UI: 告警恢复 - {record.Description}");
    }

    /// <summary>
    /// 确认告警
    /// </summary>
    private void OnAcknowledge(AlarmRecord? record)
    {
        if (record != null)
        {
            _alarmDetectionService.AcknowledgeAlarm(record);

            System.Windows.MessageBox.Show(
                $"已确认告警：{record.Description}\n\n注意：告警仍处于活动状态，只有当测量值恢复正常后，告警才会自动移到历史记录。",
                "确认成功",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 确认所有告警
    /// </summary>
    private void OnAcknowledgeAll()
    {
        var count = ActiveAlarms.Count;
        if (count == 0)
        {
            System.Windows.MessageBox.Show(
                "当前没有活动告警需要确认。",
                "提示",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        _alarmDetectionService.AcknowledgeAllAlarms();

        System.Windows.MessageBox.Show(
            $"已确认所有 {count} 个活动告警。\n\n注意：告警仍处于活动状态，只有当测量值恢复正常后，告警才会自动移到历史记录。",
            "全部确认成功",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// 清空历史告警
    /// </summary>
    private void OnClearHistory()
    {
        var message = System.Windows.Application.Current?.TryFindResource("Confirm.ClearHistory") as string
                      ?? "确定要清空所有历史告警记录吗？";

        var dialog = new Views.ConfirmationDialog(message)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        dialog.ShowDialog();

        if (dialog.Result)
        {
            _alarmDetectionService.ClearHistory();
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
