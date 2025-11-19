using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MqttMonitor.Models;

namespace MqttMonitor.Converters;

/// <summary>
/// 告警状态转文本转换器
/// </summary>
public class AlarmStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AlarmStatus alarmStatus)
        {
            var resourceKey = alarmStatus switch
            {
                AlarmStatus.Active => "AlarmStatus.Active",
                AlarmStatus.Recovered => "AlarmStatus.Recovered",
                _ => "AlarmStatus.Unknown"
            };

            return Application.Current?.TryFindResource(resourceKey) as string ?? resourceKey;
        }
        return Application.Current?.TryFindResource("AlarmStatus.Unknown") as string ?? "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
