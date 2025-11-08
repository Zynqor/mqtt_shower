using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 提醒设置 ViewModel
/// </summary>
public class AlertSettingsViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private readonly AlarmConfigService _alarmConfigService;
    private readonly AlertSettings _alertSettings;

    private bool _soundEnabled;
    private int _soundVolume;
    private int _defaultRepeatMinutes;
    private bool _repeatSound;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnSettingsSaved;
    public event Action? OnCancelled;

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

    // 命令
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand TestAlarmSoundCommand { get; }
    public ICommand TestRecoverySoundCommand { get; }

    public AlertSettingsViewModel(LogService logService, AlarmConfigService alarmConfigService, AlertSettings alertSettings)
    {
        _logService = logService;
        _alarmConfigService = alarmConfigService;
        _alertSettings = alertSettings;

        // 初始化命令
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);
        TestAlarmSoundCommand = new RelayCommand(OnTestAlarmSound);
        TestRecoverySoundCommand = new RelayCommand(OnTestRecoverySound);

        // 加载配置
        LoadSettings();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        SoundEnabled = _alertSettings.SoundEnabled;
        SoundVolume = _alertSettings.SoundVolume;
        DefaultRepeatMinutes = _alertSettings.DefaultRepeatMinutes;
        RepeatSound = _alertSettings.RepeatSound;
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private void OnSave()
    {
        try
        {
            _alertSettings.SoundEnabled = SoundEnabled;
            _alertSettings.SoundVolume = SoundVolume;
            _alertSettings.DefaultRepeatMinutes = DefaultRepeatMinutes;
            _alertSettings.RepeatSound = RepeatSound;

            _alarmConfigService.SaveAlertSettings(_alertSettings);

            _logService.LogInfo("提醒设置已保存");
            OnSettingsSaved?.Invoke();
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存提醒设置失败");
            System.Windows.MessageBox.Show($"保存失败：{ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 取消
    /// </summary>
    private void OnCancel()
    {
        OnCancelled?.Invoke();
    }

    /// <summary>
    /// 测试告警音
    /// </summary>
    private void OnTestAlarmSound()
    {
        try
        {
            System.Media.SystemSounds.Exclamation.Play();
            _logService.LogInfo("播放测试告警音");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "播放测试音效失败");
        }
    }

    /// <summary>
    /// 测试恢复音
    /// </summary>
    private void OnTestRecoverySound()
    {
        try
        {
            System.Media.SystemSounds.Asterisk.Play();
            _logService.LogInfo("播放测试恢复音");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "播放测试音效失败");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
