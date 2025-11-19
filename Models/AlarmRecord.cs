using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MqttMonitor.Models;

/// <summary>
/// 告警记录
/// </summary>
public class AlarmRecord : INotifyPropertyChanged
{
    private AlarmStatus _status = AlarmStatus.Active;
    private bool _acknowledged = false;
    private DateTime? _recoveredTime;
    private DateTime? _lastSoundTime;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 记录ID（唯一标识）
    /// </summary>
    public string RecordId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 测点名称
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// 触发时间
    /// </summary>
    public DateTime TriggerTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 告警类型（上限/下限）
    /// </summary>
    public AlarmType AlarmType { get; set; }

    /// <summary>
    /// 触发值
    /// </summary>
    public double TriggerValue { get; set; }

    /// <summary>
    /// 阈值（上限或下限）
    /// </summary>
    public double ThresholdValue { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 告警状态
    /// </summary>
    public AlarmStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>
    /// 是否已确认
    /// </summary>
    public bool Acknowledged
    {
        get => _acknowledged;
        set
        {
            if (_acknowledged != value)
            {
                _acknowledged = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 恢复时间
    /// </summary>
    public DateTime? RecoveredTime
    {
        get => _recoveredTime;
        set
        {
            if (_recoveredTime != value)
            {
                _recoveredTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }
    }

    /// <summary>
    /// 上次播放声音的时间
    /// </summary>
    public DateTime? LastSoundTime
    {
        get => _lastSoundTime;
        set
        {
            if (_lastSoundTime != value)
            {
                _lastSoundTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 持续时长
    /// </summary>
    public string Duration
    {
        get
        {
            var endTime = RecoveredTime ?? DateTime.Now;
            var duration = endTime - TriggerTime;

            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}小时{duration.Minutes}分钟";
            else if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes}分{duration.Seconds}秒";
            else
                return $"{(int)duration.TotalSeconds}秒";
        }
    }

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText
    {
        get
        {
            var resourceKey = Status switch
            {
                AlarmStatus.Active => "AlarmStatus.Active",
                AlarmStatus.Recovered => "AlarmStatus.Recovered",
                _ => "AlarmStatus.Unknown"
            };
            return Application.Current?.TryFindResource(resourceKey) as string ?? resourceKey;
        }
    }

    /// <summary>
    /// 告警描述
    /// </summary>
    public string Description
    {
        get
        {
            var resourceKey = AlarmType == AlarmType.UpperLimit
                ? "AlarmDescription.ExceedsUpperLimit"
                : "AlarmDescription.BelowLowerLimit";
            var typeText = Application.Current?.TryFindResource(resourceKey) as string ?? resourceKey;
            return $"{DeviceId} - {MetricName} {typeText}";
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 告警类型
/// </summary>
public enum AlarmType
{
    /// <summary>
    /// 上限告警
    /// </summary>
    UpperLimit,

    /// <summary>
    /// 下限告警
    /// </summary>
    LowerLimit
}

/// <summary>
/// 告警状态
/// </summary>
public enum AlarmStatus
{
    /// <summary>
    /// 活动中（持续超限）
    /// </summary>
    Active,

    /// <summary>
    /// 已恢复（数值回到正常范围）
    /// </summary>
    Recovered
}
