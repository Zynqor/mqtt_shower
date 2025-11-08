using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MqttMonitor.Models;

/// <summary>
/// 告警提醒设置
/// </summary>
public class AlertSettings : INotifyPropertyChanged
{
    private bool _soundEnabled = true;
    private int _soundVolume = 80;
    private int _defaultRepeatMinutes = 5;
    private bool _repeatSound = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 启用声音提醒
    /// </summary>
    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            if (_soundEnabled != value)
            {
                _soundEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 音量 (0-100)
    /// </summary>
    public int SoundVolume
    {
        get => _soundVolume;
        set
        {
            if (_soundVolume != value)
            {
                _soundVolume = Math.Max(0, Math.Min(100, value));
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 默认重复间隔（分钟）
    /// </summary>
    public int DefaultRepeatMinutes
    {
        get => _defaultRepeatMinutes;
        set
        {
            if (_defaultRepeatMinutes != value)
            {
                _defaultRepeatMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否重复播放声音
    /// </summary>
    public bool RepeatSound
    {
        get => _repeatSound;
        set
        {
            if (_repeatSound != value)
            {
                _repeatSound = value;
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
