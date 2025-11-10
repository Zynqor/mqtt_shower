using System;
using System.Globalization;
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
            return alarmStatus switch
            {
                AlarmStatus.Active => "进行中",
                AlarmStatus.Recovered => "已恢复",
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
