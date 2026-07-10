using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ZlinksPackageSystem.Desktop.Converters
{
    /// <summary>
    /// 布尔值转颜色画刷：true=绿色（成功），false=红色（不可用）
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool available = value is bool b && b;
            var color = available ? "#FF52C41A" : "#FFF56C6C";
            return new SolidColorBrush(Color.Parse(color));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 字符串为空/未检测时返回灰色画刷，否则正常文本色
    /// </summary>
    public class StringEmptyToDimConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool empty = string.IsNullOrWhiteSpace(value as string);
            var color = empty ? "#66FFFFFF" : "#FFBFcbd9";
            return new SolidColorBrush(Color.Parse(color));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}