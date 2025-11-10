using System;
using System.Collections.Generic;
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
/// 告警配置 ViewModel
/// </summary>
public class AlarmConfigViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private readonly AlarmConfigService _alarmConfigService;
    private readonly DataProcessingService _dataProcessingService;
    private readonly AlarmDetectionService _alarmDetectionService;

    private ObservableCollection<DeviceMetricGroup> _deviceGroups = new();
    private DeviceMetricItem? _selectedMetric;
    private AlarmConfig? _currentConfig;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnConfigSaved;
    public event Action? OnCancelled;

    /// <summary>
    /// 设备分组
    /// </summary>
    public ObservableCollection<DeviceMetricGroup> DeviceGroups
    {
        get => _deviceGroups;
        set
        {
            _deviceGroups = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 选中的测点
    /// </summary>
    public DeviceMetricItem? SelectedMetric
    {
        get => _selectedMetric;
        set
        {
            if (_selectedMetric != value)
            {
                _selectedMetric = value;
                OnPropertyChanged();
                LoadConfigForSelectedMetric();
            }
        }
    }

    /// <summary>
    /// 当前配置
    /// </summary>
    public AlarmConfig? CurrentConfig
    {
        get => _currentConfig;
        set
        {
            _currentConfig = value;
            OnPropertyChanged();
        }
    }

    // 命令
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand EnableAllCommand { get; }
    public ICommand DisableAllCommand { get; }

    public AlarmConfigViewModel(
        LogService logService,
        AlarmConfigService alarmConfigService,
        DataProcessingService dataProcessingService,
        AlarmDetectionService alarmDetectionService)
    {
        _logService = logService;
        _alarmConfigService = alarmConfigService;
        _dataProcessingService = dataProcessingService;
        _alarmDetectionService = alarmDetectionService;

        // 初始化命令
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);
        EnableAllCommand = new RelayCommand(OnEnableAll);
        DisableAllCommand = new RelayCommand(OnDisableAll);

        // 加载数据
        LoadDeviceMetrics();
    }

    /// <summary>
    /// 加载设备和测点
    /// </summary>
    private void LoadDeviceMetrics()
    {
        var configs = _alarmConfigService.LoadAlarmConfigs();
        var groups = new List<DeviceMetricGroup>();

        // 从已接收的数据中获取设备和测点
        foreach (var deviceKvp in _dataProcessingService.AllDeviceData)
        {
            var deviceId = deviceKvp.Key;
            var deviceState = deviceKvp.Value;

            var metrics = new List<DeviceMetricItem>();

            foreach (var metricKvp in deviceState.LatestMetrics)
            {
                var metricName = metricKvp.Key;
                var metric = metricKvp.Value;

                // 获取或创建配置
                var config = configs.FirstOrDefault(c => c.DeviceId == deviceId && c.MetricName == metricName);
                if (config == null)
                {
                    config = new AlarmConfig
                    {
                        DeviceId = deviceId,
                        MetricName = metricName,
                        Enabled = false
                    };
                    configs.Add(config);
                }

                metrics.Add(new DeviceMetricItem
                {
                    DeviceId = deviceId,
                    MetricName = metricName,
                    Unit = metric.Unit ?? "",
                    HasAlarm = config.Enabled
                });
            }

            if (metrics.Any())
            {
                // 按测点名称排序（递增）
                var sortedMetrics = metrics.OrderBy(m => m.MetricName).ToList();

                groups.Add(new DeviceMetricGroup
                {
                    DeviceId = deviceId,
                    Metrics = new ObservableCollection<DeviceMetricItem>(sortedMetrics)
                });
            }
        }

        DeviceGroups = new ObservableCollection<DeviceMetricGroup>(groups.OrderBy(g => g.DeviceId));
    }

    /// <summary>
    /// 加载选中测点的配置
    /// </summary>
    private void LoadConfigForSelectedMetric()
    {
        if (SelectedMetric == null)
        {
            CurrentConfig = null;
            return;
        }

        var configs = _alarmConfigService.LoadAlarmConfigs();
        var config = configs.FirstOrDefault(c =>
            c.DeviceId == SelectedMetric.DeviceId &&
            c.MetricName == SelectedMetric.MetricName);

        if (config == null)
        {
            config = new AlarmConfig
            {
                DeviceId = SelectedMetric.DeviceId,
                MetricName = SelectedMetric.MetricName,
                Enabled = false
            };
        }

        CurrentConfig = new AlarmConfig
        {
            DeviceId = config.DeviceId,
            MetricName = config.MetricName,
            Enabled = config.Enabled,
            EnableUpperLimit = config.EnableUpperLimit,
            UpperLimit = config.UpperLimit,
            EnableLowerLimit = config.EnableLowerLimit,
            LowerLimit = config.LowerLimit,
            DurationSeconds = config.DurationSeconds,
            EnableVisualAlert = config.EnableVisualAlert,
            EnableSoundAlert = config.EnableSoundAlert,
            SoundRepeatMinutes = config.SoundRepeatMinutes
        };
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void OnSave()
    {
        try
        {
            var configs = _alarmConfigService.LoadAlarmConfigs();

            // 更新当前选中的配置
            if (CurrentConfig != null && SelectedMetric != null)
            {
                var existing = configs.FirstOrDefault(c =>
                    c.DeviceId == CurrentConfig.DeviceId &&
                    c.MetricName == CurrentConfig.MetricName);

                if (existing != null)
                {
                    configs.Remove(existing);
                }

                configs.Add(CurrentConfig);
            }

            _alarmConfigService.SaveAlarmConfigs(configs);
            _alarmDetectionService.LoadConfigs(); // 重新加载配置

            // 更新UI中的HasAlarm状态（立即显示感叹号图标）
            if (SelectedMetric != null && CurrentConfig != null)
            {
                SelectedMetric.HasAlarm = CurrentConfig.Enabled;
            }

            _logService.LogInfo("告警配置已保存");

            // 显示保存成功提示（不关闭窗口，方便继续配置其他测点）
            System.Windows.MessageBox.Show("告警配置已保存！", "成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存告警配置失败");
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
    /// 启用所有告警
    /// </summary>
    private void OnEnableAll()
    {
        var configs = _alarmConfigService.LoadAlarmConfigs();
        foreach (var config in configs)
        {
            config.Enabled = true;
        }
        _alarmConfigService.SaveAlarmConfigs(configs);
        _alarmDetectionService.LoadConfigs();
        LoadDeviceMetrics();
        _logService.LogInfo("已启用所有告警");
    }

    /// <summary>
    /// 禁用所有告警
    /// </summary>
    private void OnDisableAll()
    {
        var configs = _alarmConfigService.LoadAlarmConfigs();
        foreach (var config in configs)
        {
            config.Enabled = false;
        }
        _alarmConfigService.SaveAlarmConfigs(configs);
        _alarmDetectionService.LoadConfigs();
        LoadDeviceMetrics();
        _logService.LogInfo("已禁用所有告警");
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 设备测点分组
/// </summary>
public class DeviceMetricGroup
{
    public string DeviceId { get; set; } = string.Empty;
    public ObservableCollection<DeviceMetricItem> Metrics { get; set; } = new();
}

/// <summary>
/// 设备测点项
/// </summary>
public class DeviceMetricItem : INotifyPropertyChanged
{
    private bool _hasAlarm;

    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public bool HasAlarm
    {
        get => _hasAlarm;
        set
        {
            if (_hasAlarm != value)
            {
                _hasAlarm = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string DisplayName => $"{MetricName} {(HasAlarm ? "⚠️" : "")}";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
