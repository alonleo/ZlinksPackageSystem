using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ZlinksPackageSystem.Desktop.Converters
{
    /// <summary>
    /// 枚举 → int（给下拉框 SelectedIndex 用）
    /// </summary>
    public class EnumToIntConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            return (int)value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int i && targetType.IsEnum)
                return Enum.ToObject(targetType, i);
            return 0;
        }
    }
}
