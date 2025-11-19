using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 历史告警查询窗口 ViewModel
/// </summary>
public class AlarmHistoryQueryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly LogService _logService;
    private readonly AlarmDatabaseService _alarmDatabaseService;
    private DateTime _startDateTime = DateTime.Today;
    private DateTime _endDateTime = DateTime.Now;
    private string _selectedDevice = GetResourceString("AlarmHistoryQuery.AllDevices");
    private string _selectedAlarmType = GetResourceString("AlarmHistoryQuery.AllTypes");
    private ObservableCollection<string> _availableDevices = new();
    private ObservableCollection<AlarmRecord> _alarmRecords = new();
    private bool _isLoading = false;
    private string _statusMessage = "就绪";
    private CancellationTokenSource? _queryCts;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartDateTime
    {
        get => _startDateTime;
        set
        {
            if (_startDateTime != value)
            {
                _startDateTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndDateTime
    {
        get => _endDateTime;
        set
        {
            if (_endDateTime != value)
            {
                _endDateTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 选中的设备
    /// </summary>
    public string SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (_selectedDevice != value)
            {
                _selectedDevice = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 选中的告警类型
    /// </summary>
    public string SelectedAlarmType
    {
        get => _selectedAlarmType;
        set
        {
            if (_selectedAlarmType != value)
            {
                _selectedAlarmType = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 可用的设备列表
    /// </summary>
    public ObservableCollection<string> AvailableDevices
    {
        get => _availableDevices;
        set
        {
            if (_availableDevices != value)
            {
                _availableDevices = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 告警类型列表
    /// </summary>
    public ObservableCollection<string> AlarmTypes { get; }

    private static string GetResourceString(string key, string fallback = "")
    {
        return System.Windows.Application.Current?.TryFindResource(key) as string ?? fallback;
    }

    /// <summary>
    /// 告警记录
    /// </summary>
    public ObservableCollection<AlarmRecord> AlarmRecords
    {
        get => _alarmRecords;
        set
        {
            if (_alarmRecords != value)
            {
                _alarmRecords = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    // 命令
    public ICommand QueryCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RefreshDevicesCommand { get; }

    public AlarmHistoryQueryViewModel(LogService logService, AlarmDatabaseService alarmDatabaseService)
    {
        _logService = logService;
        _alarmDatabaseService = alarmDatabaseService;

        // 初始化告警类型列表
        AlarmTypes = new ObservableCollection<string>
        {
            GetResourceString("AlarmHistoryQuery.AllTypes", "全部"),
            GetResourceString("AlarmType.UpperLimit", "上限告警"),
            GetResourceString("AlarmType.LowerLimit", "下限告警")
        };

        // 初始化命令
        QueryCommand = new RelayCommand(OnQuery);
        ExportCommand = new RelayCommand(OnExport, CanExport);
        RefreshDevicesCommand = new RelayCommand(OnRefreshDevices);

        // 加载设备列表
        LoadAvailableDevices();
    }

    /// <summary>
    /// 加载可用的设备列表
    /// </summary>
    private async void LoadAvailableDevices()
    {
        try
        {
            var devices = await _alarmDatabaseService.GetAllDeviceIdsAsync();

            var allDevicesText = GetResourceString("AlarmHistoryQuery.AllDevices", "全部");
            AvailableDevices = new ObservableCollection<string>(new[] { allDevicesText }.Concat(devices.OrderBy(d => d)));

            if (AvailableDevices.Count > 0 && SelectedDevice == allDevicesText)
            {
                // 保持"全部"选中
            }

            _logService.LogInfo($"加载了 {devices.Count} 个有告警记录的设备");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载设备列表失败");
        }
    }

    /// <summary>
    /// 刷新设备列表
    /// </summary>
    private void OnRefreshDevices()
    {
        LoadAvailableDevices();
        StatusMessage = GetResourceString("Status.DeviceListRefreshed", "设备列表已刷新");
    }

    /// <summary>
    /// 查询历史告警
    /// </summary>
    private async void OnQuery()
    {
        if (StartDateTime > EndDateTime)
        {
            StatusMessage = GetResourceString("Error.InvalidDateRange", "开始时间不能晚于结束时间");
            return;
        }

        // 取消之前的查询
        _queryCts?.Cancel();
        _queryCts?.Dispose();
        _queryCts = new CancellationTokenSource();
        var token = _queryCts.Token;

        try
        {
            IsLoading = true;
            StatusMessage = GetResourceString("Status.Querying", "正在查询...");
            AlarmRecords.Clear();

            // 确定查询的设备ID
            var allDevicesText = GetResourceString("AlarmHistoryQuery.AllDevices", "全部");
            string? deviceId = SelectedDevice == allDevicesText ? null : SelectedDevice;

            // 确定查询的告警类型
            var upperLimitText = GetResourceString("AlarmType.UpperLimit", "上限告警");
            var lowerLimitText = GetResourceString("AlarmType.LowerLimit", "下限告警");
            AlarmType? alarmType = SelectedAlarmType == upperLimitText ? AlarmType.UpperLimit :
                                   SelectedAlarmType == lowerLimitText ? AlarmType.LowerLimit :
                                   null;

            // 查询数据库（使用Task.Run以支持取消）
            var records = await System.Threading.Tasks.Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                return await _alarmDatabaseService.QueryAlarmsAsync(
                    StartDateTime,
                    EndDateTime,
                    deviceId,
                    alarmType);
            }, token);

            token.ThrowIfCancellationRequested();

            AlarmRecords = new ObservableCollection<AlarmRecord>(records);

            var deviceFilter = deviceId ?? GetResourceString("AlarmHistoryQuery.AllDevices", "全部设备");
            var typeFilter = SelectedAlarmType;

            if (AlarmRecords.Count > 0)
            {
                var template = GetResourceString("Query.LoadedRecords", "已加载 {0} 条记录");
                StatusMessage = string.Format(template, AlarmRecords.Count);
            }
            else
            {
                StatusMessage = GetResourceString("Query.NoRecords", "未找到符合条件的记录");
            }

            (ExportCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = GetResourceString("Status.QueryCancelled", "查询已取消");
            _logService.LogInfo("历史告警查询被取消");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "查询历史告警失败");
            StatusMessage = $"查询失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导出数据
    /// </summary>
    private void OnExport()
    {
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV文件|*.csv",
                FileName = $"告警记录_{StartDateTime:yyyyMMdd_HHmmss}_{EndDateTime:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportToCsv(saveDialog.FileName);
                StatusMessage = $"已导出到: {saveDialog.FileName}";
                _logService.LogInfo($"导出告警数据到: {saveDialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "导出数据失败");
            StatusMessage = $"导出失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 导出到CSV
    /// </summary>
    private void ExportToCsv(string filePath)
    {
        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

        if (AlarmRecords.Count == 0)
            return;

        // 写入表头
        writer.WriteLine("触发时间,设备ID,指标名称,告警类型,触发值,阈值,单位,恢复时间,状态,已确认");

        // 写入数据
        foreach (var record in AlarmRecords)
        {
            var alarmTypeText = record.AlarmType switch
            {
                AlarmType.UpperLimit => "上限告警",
                AlarmType.LowerLimit => "下限告警",
                _ => "未知"
            };

            var statusText = record.Status switch
            {
                AlarmStatus.Active => "进行中",
                AlarmStatus.Recovered => "已恢复",
                _ => "未知"
            };

            var acknowledgedText = record.Acknowledged ? "是" : "否";
            var recoveredTime = record.RecoveredTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

            var line = $"{record.TriggerTime:yyyy-MM-dd HH:mm:ss}," +
                      $"{record.DeviceId}," +
                      $"{record.MetricName}," +
                      $"{alarmTypeText}," +
                      $"{record.TriggerValue}," +
                      $"{record.ThresholdValue}," +
                      $"{record.Unit ?? ""}," +
                      $"{recoveredTime}," +
                      $"{statusText}," +
                      $"{acknowledgedText}";

            writer.WriteLine(line);
        }
    }

    private bool CanExport() => AlarmRecords.Count > 0;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // IDisposable实现
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            // 取消正在进行的查询
            _queryCts?.Cancel();
            _queryCts?.Dispose();
            _queryCts = null;

            _logService.LogInfo("AlarmHistoryQueryViewModel 已释放资源");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "AlarmHistoryQueryViewModel 释放资源时出错");
        }
    }
}
