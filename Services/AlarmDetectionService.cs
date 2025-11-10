using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MqttMonitor.Models;

namespace MqttMonitor.Services;

/// <summary>
/// 告警检测服务
/// </summary>
public class AlarmDetectionService : IDisposable
{
    private readonly LogService _logService;
    private readonly AlarmConfigService _alarmConfigService;
    private readonly SoundPlayerService _soundPlayerService;
    private readonly DataProcessingService _dataProcessingService;
    private readonly AlarmDatabaseService _alarmDatabaseService;

    private List<AlarmConfig> _alarmConfigs = new();
    private readonly ConcurrentDictionary<string, DateTime> _exceedStartTimes = new();
    private readonly ConcurrentDictionary<string, AlarmRecord> _activeAlarms = new();
    private System.Windows.Threading.DispatcherTimer? _soundRepeatTimer;
    private bool _disposed;

    public ObservableCollection<AlarmRecord> ActiveAlarms { get; } = new();
    public ObservableCollection<AlarmRecord> HistoryAlarms { get; } = new();

    /// <summary>
    /// 告警触发事件
    /// </summary>
    public event Action<AlarmRecord>? OnAlarmTriggered;

    /// <summary>
    /// 告警恢复事件
    /// </summary>
    public event Action<AlarmRecord>? OnAlarmRecovered;

    public AlarmDetectionService(
        LogService logService,
        AlarmConfigService alarmConfigService,
        SoundPlayerService soundPlayerService,
        DataProcessingService dataProcessingService,
        AlarmDatabaseService alarmDatabaseService)
    {
        _logService = logService;
        _alarmConfigService = alarmConfigService;
        _soundPlayerService = soundPlayerService;
        _dataProcessingService = dataProcessingService;
        _alarmDatabaseService = alarmDatabaseService;

        // 订阅数据处理事件
        _dataProcessingService.OnUpstreamDataParsed += CheckAlarms;

        // 加载告警配置
        LoadConfigs();

        // 加载历史告警记录
        LoadHistoryFromDatabase();

        // 启动定时器检查音效重复
        StartSoundRepeatTimer();
    }

    /// <summary>
    /// 加载告警配置
    /// </summary>
    public void LoadConfigs()
    {
        _alarmConfigs = _alarmConfigService.LoadAlarmConfigs();
        _logService.LogInfo($"告警检测服务已加载 {_alarmConfigs.Count} 个配置");
    }

    /// <summary>
    /// 从数据库加载历史告警记录
    /// </summary>
    private async void LoadHistoryFromDatabase()
    {
        try
        {
            var recentAlarms = await _alarmDatabaseService.GetRecentAlarmsAsync(50);
            Application.Current.Dispatcher.Invoke(() =>
            {
                HistoryAlarms.Clear();
                foreach (var alarm in recentAlarms)
                {
                    HistoryAlarms.Add(alarm);
                }
            });
            _logService.LogInfo($"已从数据库加载 {recentAlarms.Count} 条历史告警记录");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "从数据库加载历史告警记录失败");
        }
    }

    /// <summary>
    /// 检查告警
    /// </summary>
    private void CheckAlarms(UpstreamDataPacket dataPacket)
    {
        if (dataPacket.Payload == null)
            return;

        foreach (var metric in dataPacket.Payload)
        {
            var key = $"{dataPacket.DeviceId}_{metric.Name}";
            var value = metric.Value;

            var config = _alarmConfigService.GetAlarmConfig(dataPacket.DeviceId, metric.Name, _alarmConfigs);

            // 如果告警被禁用，检查是否有活动告警需要恢复
            if (config == null || !config.Enabled)
            {
                // 清除超限记录
                _exceedStartTimes.TryRemove(key, out _);

                // 如果有活动告警，则恢复（用户禁用了告警配置）
                if (_activeAlarms.TryGetValue(key, out var activeAlarm))
                {
                    RecoverAlarm(activeAlarm);
                }
                continue;
            }

            // 检查是否超限
            bool isExceeding = false;
            AlarmType? alarmType = null;
            double thresholdValue = 0;

            if (config.EnableUpperLimit && value > config.UpperLimit)
            {
                isExceeding = true;
                alarmType = AlarmType.UpperLimit;
                thresholdValue = config.UpperLimit;
            }
            else if (config.EnableLowerLimit && value < config.LowerLimit)
            {
                isExceeding = true;
                alarmType = AlarmType.LowerLimit;
                thresholdValue = config.LowerLimit;
            }

            if (isExceeding && alarmType.HasValue)
            {
                // 记录开始超限时间
                if (!_exceedStartTimes.ContainsKey(key))
                {
                    _exceedStartTimes[key] = DateTime.Now;
                }

                // 检查是否持续超限达到阈值时长
                var exceedDuration = DateTime.Now - _exceedStartTimes[key];
                if (exceedDuration.TotalSeconds >= config.DurationSeconds)
                {
                    // 触发告警
                    TriggerAlarm(dataPacket.DeviceId, metric.Name, value, alarmType.Value, thresholdValue, metric.Unit ?? "", config);
                }
            }
            else
            {
                // 数值正常，移除超限记录
                _exceedStartTimes.TryRemove(key, out _);

                // 如果有活动告警，则恢复
                if (_activeAlarms.TryGetValue(key, out var activeAlarm))
                {
                    RecoverAlarm(activeAlarm);
                }
            }
        }
    }

    /// <summary>
    /// 触发告警
    /// </summary>
    private void TriggerAlarm(string deviceId, string metricName, double value, AlarmType alarmType, double thresholdValue, string unit, AlarmConfig config)
    {
        var key = $"{deviceId}_{metricName}";

        // 如果已经有活动告警，不重复触发
        if (_activeAlarms.ContainsKey(key))
            return;

        var alarmRecord = new AlarmRecord
        {
            DeviceId = deviceId,
            MetricName = metricName,
            TriggerTime = DateTime.Now,
            AlarmType = alarmType,
            TriggerValue = value,
            ThresholdValue = thresholdValue,
            Unit = unit,
            Status = AlarmStatus.Active,
            LastSoundTime = DateTime.Now
        };

        _activeAlarms[key] = alarmRecord;

        Application.Current.Dispatcher.Invoke(() =>
        {
            ActiveAlarms.Add(alarmRecord);
        });

        // 播放告警音
        if (config.EnableSoundAlert)
        {
            _soundPlayerService.PlayAlarmSound();
        }

        // 保存到数据库（异步，不阻塞）
        _ = _alarmDatabaseService.SaveAlarmRecordAsync(alarmRecord);

        // 触发事件
        OnAlarmTriggered?.Invoke(alarmRecord);

        _logService.LogWarning($"告警触发: {alarmRecord.Description}, 当前值: {value:F2}{unit}, 阈值: {thresholdValue:F2}{unit}");
    }

    /// <summary>
    /// 恢复告警
    /// </summary>
    private void RecoverAlarm(AlarmRecord alarmRecord)
    {
        var key = $"{alarmRecord.DeviceId}_{alarmRecord.MetricName}";

        alarmRecord.Status = AlarmStatus.Recovered;
        alarmRecord.RecoveredTime = DateTime.Now;

        _activeAlarms.TryRemove(key, out _);

        Application.Current.Dispatcher.Invoke(() =>
        {
            ActiveAlarms.Remove(alarmRecord);
            HistoryAlarms.Insert(0, alarmRecord); // 添加到历史记录开头
        });

        // 播放恢复音
        _soundPlayerService.PlayRecoverySound();

        // 保存到数据库（异步，不阻塞）
        _ = _alarmDatabaseService.SaveAlarmRecordAsync(alarmRecord);

        // 触发事件
        OnAlarmRecovered?.Invoke(alarmRecord);

        _logService.LogInfo($"告警恢复: {alarmRecord.Description}, 持续时长: {alarmRecord.Duration}");
    }

    /// <summary>
    /// 确认告警（停止音效重复）
    /// </summary>
    public void AcknowledgeAlarm(AlarmRecord alarmRecord)
    {
        alarmRecord.Acknowledged = true;

        // 更新数据库
        _ = _alarmDatabaseService.SaveAlarmRecordAsync(alarmRecord);

        _logService.LogInfo($"告警已确认: {alarmRecord.Description}");
    }

    /// <summary>
    /// 确认所有告警
    /// </summary>
    public void AcknowledgeAllAlarms()
    {
        foreach (var alarm in ActiveAlarms)
        {
            alarm.Acknowledged = true;

            // 更新数据库
            _ = _alarmDatabaseService.SaveAlarmRecordAsync(alarm);
        }
        _logService.LogInfo("已确认所有活动告警");
    }

    /// <summary>
    /// 清空历史告警
    /// </summary>
    public void ClearHistory()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            HistoryAlarms.Clear();
        });
        _logService.LogInfo("已清空告警历史");
    }

    /// <summary>
    /// 启动音效重复定时器
    /// </summary>
    private void StartSoundRepeatTimer()
    {
        _soundRepeatTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1) // 每分钟检查一次
        };

        _soundRepeatTimer.Tick += (s, e) =>
        {
            foreach (var alarm in ActiveAlarms)
            {
                if (alarm.Acknowledged)
                    continue;

                var config = _alarmConfigService.GetAlarmConfig(alarm.DeviceId, alarm.MetricName, _alarmConfigs);
                if (config == null || !config.EnableSoundAlert)
                    continue;

                // 检查是否需要重复播放
                if (alarm.LastSoundTime.HasValue)
                {
                    var elapsed = DateTime.Now - alarm.LastSoundTime.Value;
                    if (elapsed.TotalMinutes >= config.SoundRepeatMinutes)
                    {
                        _soundPlayerService.PlayAlarmSound();
                        alarm.LastSoundTime = DateTime.Now;
                        _logService.LogInfo($"重复播放告警音: {alarm.Description}");
                    }
                }
            }
        };

        _soundRepeatTimer.Start();
    }

    /// <summary>
    /// 获取活动告警数量
    /// </summary>
    public int GetActiveAlarmCount()
    {
        return ActiveAlarms.Count;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // 停止定时器
        _soundRepeatTimer?.Stop();

        // 停止所有音效
        _soundPlayerService?.StopAllSounds();

        // 取消事件订阅
        _dataProcessingService.OnUpstreamDataParsed -= CheckAlarms;

        _logService.LogInfo("AlarmDetectionService 已释放资源");
    }
}
