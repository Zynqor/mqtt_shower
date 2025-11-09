using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 告警统计 ViewModel
/// </summary>
public class AlarmStatisticsViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private readonly AlarmDatabaseService _alarmDatabaseService;
    private DateTime _startTime = DateTime.Today;
    private DateTime _endTime = DateTime.Now;
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            if (_startTime != value)
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime
    {
        get => _endTime;
        set
        {
            if (_endTime != value)
            {
                _endTime = value;
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
    /// 告警次数统计（设备/测点 -> 次数）
    /// </summary>
    public ObservableCollection<AlarmCountItem> AlarmCounts { get; } = new();

    /// <summary>
    /// 告警趋势数据（时间 -> 次数）
    /// </summary>
    public ObservableCollection<AlarmTrendItem> AlarmTrends { get; } = new();

    /// <summary>
    /// 告警类型分布
    /// </summary>
    public ObservableCollection<AlarmTypeDistribution> AlarmTypeDistributions { get; } = new();

    /// <summary>
    /// Top告警设备列表
    /// </summary>
    public ObservableCollection<TopAlarmDevice> TopAlarmDevices { get; } = new();

    // 命令
    public ICommand QueryCommand { get; }
    public ICommand SetTodayCommand { get; }
    public ICommand SetLast7DaysCommand { get; }
    public ICommand SetLast30DaysCommand { get; }

    public AlarmStatisticsViewModel(LogService logService, AlarmDatabaseService alarmDatabaseService)
    {
        _logService = logService;
        _alarmDatabaseService = alarmDatabaseService;

        // 初始化命令
        QueryCommand = new AsyncRelayCommand(LoadStatisticsAsync);
        SetTodayCommand = new RelayCommand(SetToday);
        SetLast7DaysCommand = new RelayCommand(SetLast7Days);
        SetLast30DaysCommand = new RelayCommand(SetLast30Days);
    }

    /// <summary>
    /// 设置为今天
    /// </summary>
    private void SetToday()
    {
        StartTime = DateTime.Today;
        EndTime = DateTime.Now;
        _ = LoadStatisticsAsync();
    }

    /// <summary>
    /// 设置为最近7天
    /// </summary>
    private void SetLast7Days()
    {
        StartTime = DateTime.Today.AddDays(-7);
        EndTime = DateTime.Now;
        _ = LoadStatisticsAsync();
    }

    /// <summary>
    /// 设置为最近30天
    /// </summary>
    private void SetLast30Days()
    {
        StartTime = DateTime.Today.AddDays(-30);
        EndTime = DateTime.Now;
        _ = LoadStatisticsAsync();
    }

    /// <summary>
    /// 加载统计数据
    /// </summary>
    public async Task LoadStatisticsAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;

        try
        {
            // 查询指定时间范围的告警记录
            var alarms = await _alarmDatabaseService.GetAlarmsByDateRangeAsync(StartTime, EndTime);

            _logService.LogInfo($"查询时间范围: {StartTime:yyyy-MM-dd HH:mm:ss} - {EndTime:yyyy-MM-dd HH:mm:ss}");
            _logService.LogInfo($"已加载告警统计数据: {alarms.Count} 条记录");

            // 计算各种统计数据
            CalculateAlarmCounts(alarms);
            CalculateAlarmTrends(alarms);
            CalculateAlarmTypeDistributions(alarms);
            CalculateTopAlarmDevices(alarms);

            _logService.LogInfo($"统计完成 - 告警次数:{AlarmCounts.Count}, 趋势:{AlarmTrends.Count}, 类型:{AlarmTypeDistributions.Count}, Top设备:{TopAlarmDevices.Count}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载告警统计数据失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 计算告警次数统计
    /// </summary>
    private void CalculateAlarmCounts(List<AlarmRecord> alarms)
    {
        AlarmCounts.Clear();

        var grouped = alarms.GroupBy(a => new { a.DeviceId, a.MetricName })
            .Select(g => new AlarmCountItem
            {
                DeviceId = g.Key.DeviceId,
                MetricName = g.Key.MetricName,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(20); // 只显示前20个

        foreach (var item in grouped)
        {
            AlarmCounts.Add(item);
        }
    }

    /// <summary>
    /// 计算告警趋势（按天）
    /// </summary>
    private void CalculateAlarmTrends(List<AlarmRecord> alarms)
    {
        AlarmTrends.Clear();

        var daysDiff = (EndTime - StartTime).TotalDays;

        if (daysDiff <= 1)
        {
            // 按小时统计
            var grouped = alarms.GroupBy(a => new DateTime(a.TriggerTime.Year, a.TriggerTime.Month, a.TriggerTime.Day, a.TriggerTime.Hour, 0, 0))
                .Select(g => new AlarmTrendItem
                {
                    Time = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Time);

            foreach (var item in grouped)
            {
                AlarmTrends.Add(item);
            }
        }
        else
        {
            // 按天统计
            var grouped = alarms.GroupBy(a => a.TriggerTime.Date)
                .Select(g => new AlarmTrendItem
                {
                    Time = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Time);

            foreach (var item in grouped)
            {
                AlarmTrends.Add(item);
            }
        }
    }

    /// <summary>
    /// 计算告警类型分布
    /// </summary>
    private void CalculateAlarmTypeDistributions(List<AlarmRecord> alarms)
    {
        AlarmTypeDistributions.Clear();

        var upperCount = alarms.Count(a => a.AlarmType == AlarmType.UpperLimit);
        var lowerCount = alarms.Count(a => a.AlarmType == AlarmType.LowerLimit);
        var total = alarms.Count;

        if (total > 0)
        {
            AlarmTypeDistributions.Add(new AlarmTypeDistribution
            {
                TypeName = "上限告警",
                Count = upperCount,
                Percentage = (double)upperCount / total * 100
            });

            AlarmTypeDistributions.Add(new AlarmTypeDistribution
            {
                TypeName = "下限告警",
                Count = lowerCount,
                Percentage = (double)lowerCount / total * 100
            });
        }
    }

    /// <summary>
    /// 计算Top告警设备
    /// </summary>
    private void CalculateTopAlarmDevices(List<AlarmRecord> alarms)
    {
        TopAlarmDevices.Clear();

        var grouped = alarms.GroupBy(a => new { a.DeviceId, a.MetricName })
            .Select(g =>
            {
                var recoveredAlarms = g.Where(x => x.RecoveredTime.HasValue).ToList();
                var avgDuration = recoveredAlarms.Any()
                    ? recoveredAlarms.Average(x => (x.RecoveredTime!.Value - x.TriggerTime).TotalMinutes)
                    : 0.0;

                return new
                {
                    g.Key.DeviceId,
                    g.Key.MetricName,
                    Count = g.Count(),
                    AvgDuration = avgDuration
                };
            })
            .OrderByDescending(x => x.Count)
            .Take(10);

        int rank = 1;
        foreach (var item in grouped)
        {
            TopAlarmDevices.Add(new TopAlarmDevice
            {
                Rank = rank++,
                DeviceId = item.DeviceId,
                MetricName = item.MetricName,
                AlarmCount = item.Count,
                AvgDuration = item.AvgDuration > 0 ? $"{item.AvgDuration:F1}分钟" : "N/A"
            });
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 告警次数统计项
/// </summary>
public class AlarmCountItem
{
    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Label => $"{DeviceId}/{MetricName}";
}

/// <summary>
/// 告警趋势项
/// </summary>
public class AlarmTrendItem
{
    public DateTime Time { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// 告警类型分布
/// </summary>
public class AlarmTypeDistribution
{
    public string TypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Top告警设备
/// </summary>
public class TopAlarmDevice
{
    public int Rank { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public int AlarmCount { get; set; }
    public string AvgDuration { get; set; } = string.Empty;
}
