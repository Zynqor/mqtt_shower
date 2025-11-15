using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 设备列表 ViewModel
/// </summary>
public class DeviceListViewModel : INotifyPropertyChanged
{
    private readonly DeviceManagementService _deviceManagementService;
    private readonly LogService _logService;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备列表
    /// </summary>
    public ObservableCollection<DeviceInfo> Devices => _deviceManagementService.DeviceList;

    /// <summary>
    /// 心跳Topic
    /// </summary>
    public string HeartbeatTopic
    {
        get => _deviceManagementService.HeartbeatTopic;
        set
        {
            if (_deviceManagementService.HeartbeatTopic != value)
            {
                _deviceManagementService.HeartbeatTopic = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 心跳超时时间（秒）
    /// </summary>
    public int HeartbeatTimeoutSeconds
    {
        get => _deviceManagementService.HeartbeatTimeoutSeconds;
        set
        {
            if (_deviceManagementService.HeartbeatTimeoutSeconds != value)
            {
                _deviceManagementService.HeartbeatTimeoutSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已启用设备管理
    /// </summary>
    public bool IsEnabled => _deviceManagementService.IsEnabled;

    /// <summary>
    /// 在线设备数量
    /// </summary>
    public int OnlineDeviceCount => _deviceManagementService.GetOnlineDeviceCount();

    /// <summary>
    /// 总设备数量
    /// </summary>
    public int TotalDeviceCount => _deviceManagementService.GetTotalDeviceCount();

    /// <summary>
    /// 统计文本
    /// </summary>
    public string StatisticsText => $"在线: {OnlineDeviceCount} / 总计: {TotalDeviceCount}";

    // 命令
    public ICommand RefreshCommand { get; }

    public DeviceListViewModel(DeviceManagementService deviceManagementService, LogService logService)
    {
        _deviceManagementService = deviceManagementService;
        _logService = logService;

        // 订阅设备管理服务的属性变化
        _deviceManagementService.PropertyChanged += OnDeviceManagementServicePropertyChanged;

        // 初始化命令
        RefreshCommand = new RelayCommand(OnRefresh);

        // 启动定时器定期刷新统计信息
        var timer = new System.Timers.Timer(5000); // 每5秒刷新一次
        timer.Elapsed += (s, e) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(OnlineDeviceCount));
                OnPropertyChanged(nameof(TotalDeviceCount));
                OnPropertyChanged(nameof(StatisticsText));
            });
        };
        timer.Start();
    }

    private void OnDeviceManagementServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceManagementService.DeviceList))
        {
            OnPropertyChanged(nameof(Devices));
            OnPropertyChanged(nameof(OnlineDeviceCount));
            OnPropertyChanged(nameof(TotalDeviceCount));
            OnPropertyChanged(nameof(StatisticsText));
        }
        else if (e.PropertyName == nameof(DeviceManagementService.IsEnabled))
        {
            OnPropertyChanged(nameof(IsEnabled));
        }
        else if (e.PropertyName == nameof(DeviceManagementService.HeartbeatTopic))
        {
            OnPropertyChanged(nameof(HeartbeatTopic));
        }
        else if (e.PropertyName == nameof(DeviceManagementService.HeartbeatTimeoutSeconds))
        {
            OnPropertyChanged(nameof(HeartbeatTimeoutSeconds));
        }
    }

    private void OnRefresh()
    {
        OnPropertyChanged(nameof(Devices));
        OnPropertyChanged(nameof(OnlineDeviceCount));
        OnPropertyChanged(nameof(TotalDeviceCount));
        OnPropertyChanged(nameof(StatisticsText));
        _logService.LogInfo("设备列表已刷新");
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
