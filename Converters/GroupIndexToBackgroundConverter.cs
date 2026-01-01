using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MqttMonitor.Converters;

/// <summary>
/// 将组索引转换为背景色（交替颜色）
/// </summary>
public class GroupIndexToBackgroundConverter : IValueConverter
{
    // 预定义的背景色组（表头色和数据色成对）
    private static readonly Brush[] HeaderBrushes = new[]
    {
        new SolidColorBrush(Color.FromRgb(225, 245, 254)), // 浅蓝 - 表头
        new SolidColorBrush(Color.FromRgb(232, 245, 233)), // 浅绿 - 表头
        new SolidColorBrush(Color.FromRgb(255, 243, 224)), // 浅橙 - 表头
        new SolidColorBrush(Color.FromRgb(243, 229, 245)), // 浅紫 - 表头
        new SolidColorBrush(Color.FromRgb(255, 235, 238)), // 浅红 - 表头
    };

    private static readonly Brush[] DataBrushes = new[]
    {
        new SolidColorBrush(Color.FromRgb(244, 251, 255)), // 极浅蓝 - 数据
        new SolidColorBrush(Color.FromRgb(246, 251, 246)), // 极浅绿 - 数据
        new SolidColorBrush(Color.FromRgb(255, 250, 245)), // 极浅橙 - 数据
        new SolidColorBrush(Color.FromRgb(250, 245, 251)), // 极浅紫 - 数据
        new SolidColorBrush(Color.FromRgb(255, 245, 247)), // 极浅红 - 数据
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int groupIndex && parameter is string paramStr)
        {
            // 根据组索引选择颜色（循环使用）
            int colorIndex = groupIndex % HeaderBrushes.Length;

            // parameter 为 "header" 时返回表头色，为 "data" 时返回数据色
            if (paramStr == "header")
            {
                return HeaderBrushes[colorIndex];
            }
            else if (paramStr == "data")
            {
                return DataBrushes[colorIndex];
            }
        }

        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
