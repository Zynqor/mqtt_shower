using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MqttMonitor.Converters;

/// <summary>
/// 将数量转换为Visibility，数量为0时显示（用于"暂无数据"提示）
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
