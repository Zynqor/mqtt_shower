using System;
using System.Globalization;
using System.Windows.Data;

namespace MqttMonitor.Converters;

/// <summary>
/// 将 DateTime 转换为格式化字符串
/// </summary>
public class DateTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            // 使用本地化的时间格式
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
