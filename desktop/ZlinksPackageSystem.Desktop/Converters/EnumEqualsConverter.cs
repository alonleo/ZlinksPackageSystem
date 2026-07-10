using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    /// <summary>
    /// 枚举 → bool（value 等于 parameter 时返回 true）。用于条件显示。
    /// 用法：IsVisible="{Binding EditRunMode, Converter={StaticResource EnumEqualsConverter}, ConverterParameter=LocalExecutable}"
    /// </summary>
    public class EnumEqualsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            var v = value.ToString();
            var p = parameter.ToString();
            return string.Equals(v, p, StringComparison.OrdinalIgnoreCase);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
