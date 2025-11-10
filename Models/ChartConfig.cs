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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
