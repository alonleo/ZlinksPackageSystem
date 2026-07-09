using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ZlinksPackageSystem.Desktop.Converters
{
    public class UrgencyToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "高" => new SolidColorBrush(Color.Parse("#FFF56C6C")),
                "中" => new SolidColorBrush(Color.Parse("#FFE6A23C")),
                _ => new SolidColorBrush(Color.Parse("#FF52C41A"))
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
