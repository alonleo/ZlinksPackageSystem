using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ZlinksPackageSystem.Desktop.Converters
{
    public class ReadToBackgroundConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isRead = value is true;
            return isRead
                ? new SolidColorBrush(Color.Parse("#1Affffff"))
                : new SolidColorBrush(Color.Parse("#33FFFFFF"));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
