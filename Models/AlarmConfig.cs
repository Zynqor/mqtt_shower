using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MqttMonitor.Models;

/// <summary>
/// 告警配置
/// </summary>
public class AlarmConfig : INotifyPropertyChanged
{
    private bool _enabled = true;
    private bool _enableUpperLimit = false;
    private double _upperLimit = 100.0;
    private bool _enableLowerLimit = false;
    private double _lowerLimit = 0.0;
    private int _durationSeconds = 3;
    private bool _enableVisualAlert = true;
    private bool _enableSoundAlert = true;
    private int _soundRepeatMinutes = 5;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 测点名称
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用告警
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 启用上限告警
    /// </summary>
    public bool EnableUpperLimit
    {
        get => _enableUpperLimit;
        set
        {
            if (_enableUpperLimit != value)
            {
                _enableUpperLimit = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 上限值
    /// </summary>
    public double UpperLimit
    {
        get => _upperLimit;
        set
        {
            if (_upperLimit != value)
            {
                _upperLimit = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 启用下限告警
    /// </summary>
    public bool EnableLowerLimit
    {
        get => _enableLowerLimit;
        set
        {
            if (_enableLowerLimit != value)
            {
                _enableLowerLimit = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 下限值
    /// </summary>
    public double LowerLimit
    {
        get => _lowerLimit;
        set
        {
            if (_lowerLimit != value)
            {
                _lowerLimit = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 持续时长（秒），避免瞬时波动
    /// </summary>
    public int DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (_durationSeconds != value)
            {
                _durationSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 启用视觉告警（表格变红）
    /// </summary>
    public bool EnableVisualAlert
    {
        get => _enableVisualAlert;
        set
        {
            if (_enableVisualAlert != value)
            {
                _enableVisualAlert = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 启用声音告警
    /// </summary>
    public bool EnableSoundAlert
    {
        get => _enableSoundAlert;
        set
        {
            if (_enableSoundAlert != value)
            {
                _enableSoundAlert = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 声音重复间隔（分钟）
    /// </summary>
    public int SoundRepeatMinutes
    {
        get => _soundRepeatMinutes;
        set
        {
            if (_soundRepeatMinutes != value)
            {
                _soundRepeatMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
