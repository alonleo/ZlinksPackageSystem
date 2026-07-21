using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    /// <summary>
    /// 字符串 → bool:空字符串/null 时返回 false;非空时返回 true。
    /// 用法:IsVisible="{Binding SomeString, Converter={StaticResource StringNotEmptyConverter}}"
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string s && !string.IsNullOrWhiteSpace(s);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
