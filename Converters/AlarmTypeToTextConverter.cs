using System;
using System.Globalization;
using System.Windows;
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
            var resourceKey = alarmType switch
            {
                AlarmType.UpperLimit => "AlarmType.UpperLimit",
                AlarmType.LowerLimit => "AlarmType.LowerLimit",
                _ => "AlarmType.Unknown"
            };

            return Application.Current?.TryFindResource(resourceKey) as string ?? resourceKey;
        }
        return Application.Current?.TryFindResource("AlarmType.Unknown") as string ?? "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
