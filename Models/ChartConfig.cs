using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MqttMonitor.Models;

/// <summary>
/// 图表显示配置
/// </summary>
public class ChartConfig : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _maxChartDataPoints = 1000;
    /// <summary>
    /// 图表最大数据点数量
    /// </summary>
    public int MaxChartDataPoints
    {
        get => _maxChartDataPoints;
        set
        {
            if (_maxChartDataPoints != value)
            {
                _maxChartDataPoints = value;
                OnPropertyChanged();
            }
        }
    }

    private int _chartUpdateInterval = 800;
    /// <summary>
    /// 图表更新间隔（毫秒）
    /// </summary>
    public int ChartUpdateInterval
    {
        get => _chartUpdateInterval;
        set
        {
            if (_chartUpdateInterval != value)
            {
                _chartUpdateInterval = value;
                OnPropertyChanged();
            }
        }
    }

    private int _maxColumnsPerRow = 6;
    /// <summary>
    /// 表格视图每行最大列数
    /// </summary>
    public int MaxColumnsPerRow
    {
        get => _maxColumnsPerRow;
        set
        {
            if (_maxColumnsPerRow != value)
            {
                _maxColumnsPerRow = value;
                OnPropertyChanged();
            }
        }
    }

    private int _deviceTimeoutSeconds = 300;
    /// <summary>
    /// 设备超时时间（秒），超过此时间未更新的设备将被自动清理，默认300秒（5分钟）
    /// </summary>
    public int DeviceTimeoutSeconds
    {
        get => _deviceTimeoutSeconds;
        set
        {
            if (_deviceTimeoutSeconds != value)
            {
                _deviceTimeoutSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
