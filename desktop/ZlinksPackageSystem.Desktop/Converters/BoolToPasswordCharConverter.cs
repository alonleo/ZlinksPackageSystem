using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    /// <summary>
    /// true → '\0'（明文显示）、false → '●'（掩码）
    /// </summary>
    public class BoolToPasswordCharConverter : IValueConverter
    {
        public static readonly BoolToPasswordCharConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && b ? '\0' : '●';
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}