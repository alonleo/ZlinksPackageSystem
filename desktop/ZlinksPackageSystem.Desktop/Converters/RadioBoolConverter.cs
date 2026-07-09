using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    public class RadioBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string paramStr)
            {
                return intValue == int.Parse(paramStr);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter is string paramStr)
            {
                return int.Parse(paramStr);
            }
            return BindingOperations.DoNothing;
        }
    }
}
