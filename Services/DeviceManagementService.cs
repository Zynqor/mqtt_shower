using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using MqttMonitor.Models;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MqttMonitor.Services;

/// <summary>
/// 设备管理服务，负责设备自动发现、心跳监控和自动订阅
/// </summary>
public class DeviceManagementService : INotifyPropertyChanged, IDisposable
{
    private readonly MqttService _mqttService;
    private readonly LogService _logService;
    private readonly ConcurrentDictionary<string, DeviceInfo> _devices = new();
    private ObservableCollection<DeviceInfo> _deviceList = new();
    private readonly Timer _heartbeatCheckTimer;
    private readonly object _deviceListLock = new();
    private string _heartbeatTopic = "iot/devices/heartbeat";
    private int _heartbeatTimeoutSeconds = 60; // 默认60秒超时
    private bool _isEnabled = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备列表（UI绑定）
    /// </summary>
    public ObservableCollection<DeviceInfo> DeviceList
    {
        get => _deviceList;
        private set
        {
            if (_deviceList != value)
            {
                _deviceList = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 心跳Topic
    /// </summary>
    public string HeartbeatTopic
    {
        get => _heartbeatTopic;
        set
        {
            if (_heartbeatTopic != value)
            {
                _heartbeatTopic = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 心跳超时时间（秒）
    /// </summary>
    public int HeartbeatTimeoutSeconds
    {
        get => _heartbeatTimeoutSeconds;
        set
        {
            if (_heartbeatTimeoutSeconds != value)
            {
                _heartbeatTimeoutSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        private set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public DeviceManagementService(MqttService mqttService, LogService logService)
    {
        _mqttService = mqttService;
        _logService = logService;

        // 创建心跳检查定时器（每10秒检查一次）
        _heartbeatCheckTimer = new Timer(10000);
        _heartbeatCheckTimer.Elapsed += OnHeartbeatCheckTimer;
        _heartbeatCheckTimer.AutoReset = true;
    }

    /// <summary>
    /// 启用设备管理服务
    /// </summary>
    public async Task EnableAsync()
    {
        if (IsEnabled)
        {
            _logService.LogWarning("设备管理服务已经启用");
            return;
        }

        try
        {
            _logService.LogInfo($"正在启用设备管理服务，心跳Topic: {HeartbeatTopic}");

            // 订阅MQTT消息接收事件
            _mqttService.OnMessageReceived += OnMqttMessageReceived;

            // 订阅心跳Topic（使用自动订阅）
            await _mqttService.SubscribeAutoAsync(HeartbeatTopic);

            // 启动心跳检查定时器
            _heartbeatCheckTimer.Start();

            IsEnabled = true;
            _logService.LogInfo("设备管理服务已启用");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "启用设备管理服务失败");
            throw;
        }
    }

    /// <summary>
    /// 禁用设备管理服务
    /// </summary>
    public async Task DisableAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            _logService.LogInfo("正在禁用设备管理服务...");

            // 停止心跳检查定时器
            _heartbeatCheckTimer.Stop();

            // 取消订阅所有设备的数据Topic
            var tasks = new List<Task>();
            foreach (var device in _devices.Values)
            {
                if (!string.IsNullOrEmpty(device.DataTopic))
                {
                    tasks.Add(_mqttService.UnsubscribeAutoAsync(device.DataTopic));
                }
            }
            await Task.WhenAll(tasks);

            // 取消订阅心跳Topic
            await _mqttService.UnsubscribeAutoAsync(HeartbeatTopic);

            // 取消订阅MQTT消息接收事件
            _mqttService.OnMessageReceived -= OnMqttMessageReceived;

            // 清空设备列表
            _devices.Clear();
            UpdateDeviceList();

            IsEnabled = false;
            _logService.LogInfo("设备管理服务已禁用");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "禁用设备管理服务失败");
        }
    }

    /// <summary>
    /// 处理接收到的MQTT消息
    /// </summary>
    private void OnMqttMessageReceived(MQTTnet.Client.MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;

            // 检查是否是心跳消息
            if (topic == HeartbeatTopic)
            {
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
                ProcessHeartbeatMessage(payload);
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "处理MQTT消息失败");
        }
    }

    /// <summary>
    /// 处理心跳消息
    /// </summary>
    private async void ProcessHeartbeatMessage(string payload)
    {
        try
        {
            // 解析心跳消息
            var heartbeat = JsonConvert.DeserializeObject<HeartbeatMessage>(payload);
            if (heartbeat == null || string.IsNullOrEmpty(heartbeat.DeviceId) || string.IsNullOrEmpty(heartbeat.DataTopic))
            {
                _logService.LogWarning($"无效的心跳消息: {payload}");
                return;
            }

            _logService.LogInfo($"收到设备心跳: {heartbeat.DeviceId}, Topic: {heartbeat.DataTopic}");

            // 检查设备是否已存在
            if (_devices.TryGetValue(heartbeat.DeviceId, out var existingDevice))
            {
                // 更新现有设备
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    existingDevice.LastHeartbeat = DateTime.Now;
                    existingDevice.IsOnline = true;

                    // 更新设备信息
                    if (!string.IsNullOrEmpty(heartbeat.DeviceName))
                    {
                        existingDevice.DeviceName = heartbeat.DeviceName;
                    }
                    if (!string.IsNullOrEmpty(heartbeat.DeviceType))
                    {
                        existingDevice.DeviceType = heartbeat.DeviceType;
                    }

                    // 检查DataTopic是否变化
                    if (existingDevice.DataTopic != heartbeat.DataTopic)
                    {
                        var oldTopic = existingDevice.DataTopic;
                        existingDevice.DataTopic = heartbeat.DataTopic;

                        // 取消订阅旧Topic，订阅新Topic
                        Task.Run(async () =>
                        {
                            if (!string.IsNullOrEmpty(oldTopic))
                            {
                                await _mqttService.UnsubscribeAutoAsync(oldTopic);
                                _logService.LogInfo($"设备 {heartbeat.DeviceId} 的DataTopic已变更，取消订阅旧Topic: {oldTopic}");
                            }
                            await _mqttService.SubscribeAutoAsync(heartbeat.DataTopic);
                            _logService.LogInfo($"设备 {heartbeat.DeviceId} 的DataTopic已变更，订阅新Topic: {heartbeat.DataTopic}");
                        });
                    }
                });
            }
            else
            {
                // 添加新设备
                var newDevice = new DeviceInfo
                {
                    DeviceId = heartbeat.DeviceId,
                    DeviceName = heartbeat.DeviceName ?? string.Empty,
                    DeviceType = heartbeat.DeviceType ?? string.Empty,
                    DataTopic = heartbeat.DataTopic,
                    LastHeartbeat = DateTime.Now,
                    IsOnline = true
                };

                _devices.TryAdd(heartbeat.DeviceId, newDevice);
                UpdateDeviceList();

                // 自动订阅设备的数据Topic
                await _mqttService.SubscribeAutoAsync(heartbeat.DataTopic);
                _logService.LogInfo($"新设备已加入: {heartbeat.DeviceId}, 已自动订阅: {heartbeat.DataTopic}");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "处理心跳消息失败");
        }
    }

    /// <summary>
    /// 心跳检查定时器回调
    /// </summary>
    private void OnHeartbeatCheckTimer(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var now = DateTime.Now;
            var changedDevices = new List<DeviceInfo>();

            foreach (var device in _devices.Values)
            {
                var elapsed = (now - device.LastHeartbeat).TotalSeconds;
                var wasOnline = device.IsOnline;
                var isOnline = elapsed < HeartbeatTimeoutSeconds;

                if (wasOnline != isOnline)
                {
                    changedDevices.Add(device);
                }
            }

            // 在UI线程更新设备状态
            if (changedDevices.Any())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var device in changedDevices)
                    {
                        var elapsed = (now - device.LastHeartbeat).TotalSeconds;
                        device.IsOnline = elapsed < HeartbeatTimeoutSeconds;

                        if (!device.IsOnline)
                        {
                            _logService.LogWarning($"设备 {device.DeviceId} 已离线，最后心跳: {device.LastHeartbeatText}");
                        }
                        else
                        {
                            _logService.LogInfo($"设备 {device.DeviceId} 已上线");
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "心跳检查失败");
        }
    }

    /// <summary>
    /// 更新设备列表（用于UI绑定）
    /// </summary>
    private void UpdateDeviceList()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_deviceListLock)
            {
                DeviceList = new ObservableCollection<DeviceInfo>(_devices.Values.OrderBy(d => d.DeviceId));
            }
        });
    }

    /// <summary>
    /// 获取设备信息
    /// </summary>
    public DeviceInfo? GetDevice(string deviceId)
    {
        _devices.TryGetValue(deviceId, out var device);
        return device;
    }

    /// <summary>
    /// 获取在线设备数量
    /// </summary>
    public int GetOnlineDeviceCount()
    {
        return _devices.Values.Count(d => d.IsOnline);
    }

    /// <summary>
    /// 获取总设备数量
    /// </summary>
    public int GetTotalDeviceCount()
    {
        return _devices.Count;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _heartbeatCheckTimer?.Dispose();
    }
}
