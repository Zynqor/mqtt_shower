using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MqttMonitor.Models;

/// <summary>
/// 图表图例项
/// </summary>
public class ChartLegendItem : INotifyPropertyChanged
{
    private double _offset;
    private string _deviceId = string.Empty;
    private string _parameterName = string.Empty;
    private string _colorHex = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId
    {
        get => _deviceId;
        set
        {
            _deviceId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 参数名称
    /// </summary>
    public string ParameterName
    {
        get => _parameterName;
        set
        {
            _parameterName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 颜色（十六进制格式）
    /// </summary>
    public string ColorHex
    {
        get => _colorHex;
        set
        {
            _colorHex = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 偏移量（用于纵向平移线条）
    /// </summary>
    public double Offset
    {
        get => _offset;
        set
        {
            _offset = value;
            OnPropertyChanged();
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
