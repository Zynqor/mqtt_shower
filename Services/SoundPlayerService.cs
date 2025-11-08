using System;
using System.IO;
using System.Media;
using MqttMonitor.Models;

namespace MqttMonitor.Services;

/// <summary>
/// 音效播放服务
/// </summary>
public class SoundPlayerService
{
    private readonly LogService _logService;
    private readonly AlertSettings _alertSettings;
    private SoundPlayer? _alarmPlayer;
    private SoundPlayer? _recoveryPlayer;

    public SoundPlayerService(LogService logService, AlertSettings alertSettings)
    {
        _logService = logService;
        _alertSettings = alertSettings;
        LoadSoundFiles();
    }

    /// <summary>
    /// 加载音效文件
    /// </summary>
    private void LoadSoundFiles()
    {
        try
        {
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var soundsDirectory = Path.Combine(exeDirectory, "Sounds");

            // 如果Sounds目录不存在，创建它
            if (!Directory.Exists(soundsDirectory))
            {
                Directory.CreateDirectory(soundsDirectory);
                _logService.LogInfo($"已创建音效目录: {soundsDirectory}");
            }

            var alarmSoundPath = Path.Combine(soundsDirectory, "alarm.wav");
            var recoverySoundPath = Path.Combine(soundsDirectory, "recovery.wav");

            // 加载告警音
            if (File.Exists(alarmSoundPath))
            {
                _alarmPlayer = new SoundPlayer(alarmSoundPath);
                _alarmPlayer.Load();
                _logService.LogInfo("已加载告警音效");
            }
            else
            {
                _logService.LogWarning($"告警音效文件不存在: {alarmSoundPath}，将使用系统默认提示音");
            }

            // 加载恢复音
            if (File.Exists(recoverySoundPath))
            {
                _recoveryPlayer = new SoundPlayer(recoverySoundPath);
                _recoveryPlayer.Load();
                _logService.LogInfo("已加载恢复音效");
            }
            else
            {
                _logService.LogWarning($"恢复音效文件不存在: {recoverySoundPath}，将使用系统默认提示音");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载音效文件失败");
        }
    }

    /// <summary>
    /// 播放告警音
    /// </summary>
    public void PlayAlarmSound()
    {
        if (!_alertSettings.SoundEnabled)
            return;

        try
        {
            if (_alarmPlayer != null)
            {
                _alarmPlayer.Play();
            }
            else
            {
                // 使用系统默认告警音
                SystemSounds.Exclamation.Play();
            }

            _logService.LogInfo("播放告警音效");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "播放告警音效失败");
        }
    }

    /// <summary>
    /// 播放恢复音
    /// </summary>
    public void PlayRecoverySound()
    {
        if (!_alertSettings.SoundEnabled)
            return;

        try
        {
            if (_recoveryPlayer != null)
            {
                _recoveryPlayer.Play();
            }
            else
            {
                // 使用系统默认提示音
                SystemSounds.Asterisk.Play();
            }

            _logService.LogInfo("播放恢复音效");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "播放恢复音效失败");
        }
    }

    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopAllSounds()
    {
        try
        {
            _alarmPlayer?.Stop();
            _recoveryPlayer?.Stop();
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "停止音效失败");
        }
    }
}
