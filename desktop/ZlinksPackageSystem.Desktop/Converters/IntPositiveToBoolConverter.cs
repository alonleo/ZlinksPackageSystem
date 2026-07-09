using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    public class IntPositiveToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isPositive = value is int count && count > 0;
            // parameter="invert" 时取反,用于"列表为空"的可见性绑定
            if (parameter is string s && string.Equals(s, "invert", StringComparison.OrdinalIgnoreCase))
                isPositive = !isPositive;
            return isPositive;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}