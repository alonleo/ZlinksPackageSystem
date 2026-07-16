using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    public class BoolToExpandGlyphConverter : IValueConverter
    {
        public static readonly BoolToExpandGlyphConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && b ? "\u25B2" : "\u25BC";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}