using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MqttMonitor.Models;

/// <summary>
/// 设备信息
/// </summary>
public class DeviceInfo : INotifyPropertyChanged
{
    private string _deviceId = string.Empty;
    private string _dataTopic = string.Empty;
    private DateTime _lastHeartbeat = DateTime.Now;
    private bool _isOnline = true;
    private string _deviceName = string.Empty;
    private string _deviceType = string.Empty;

    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId
    {
        get => _deviceId;
        set
        {
            if (_deviceId != value)
            {
                _deviceId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 设备名称（可选）
    /// </summary>
    public string DeviceName
    {
        get => _deviceName;
        set
        {
            if (_deviceName != value)
            {
                _deviceName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 设备类型（可选）
    /// </summary>
    public string DeviceType
    {
        get => _deviceType;
        set
        {
            if (_deviceType != value)
            {
                _deviceType = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 数据Topic
    /// </summary>
    public string DataTopic
    {
        get => _dataTopic;
        set
        {
            if (_dataTopic != value)
            {
                _dataTopic = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    public DateTime LastHeartbeat
    {
        get => _lastHeartbeat;
        set
        {
            if (_lastHeartbeat != value)
            {
                _lastHeartbeat = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastHeartbeatText));
                OnPropertyChanged(nameof(OfflineDuration));
            }
        }
    }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            if (_isOnline != value)
            {
                _isOnline = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    /// <summary>
    /// 最后心跳时间文本（用于显示）
    /// </summary>
    public string LastHeartbeatText => LastHeartbeat.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 离线时长（秒）
    /// </summary>
    public double OfflineDuration => (DateTime.Now - LastHeartbeat).TotalSeconds;

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => IsOnline ? "在线" : "离线";

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => IsOnline ? "#28A745" : "#DC3545";

    /// <summary>
    /// 显示名称（优先使用设备名称，否则使用设备ID）
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(DeviceName) ? DeviceId : DeviceName;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
