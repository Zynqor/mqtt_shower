using System;
using System.Globalization;
using System.Windows.Data;
using MqttMonitor.Models;

namespace MqttMonitor.Converters;

/// <summary>
/// 告警类型转文本转换器
/// </summary>
public class AlarmTypeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlarmType alarmType)
        {
            return alarmType switch
            {
                AlarmType.UpperLimit => "上限告警",
                AlarmType.LowerLimit => "下限告警",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
