using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OfertaDemanda.Desktop.Converters;

public sealed class BoolInvertConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag && !flag;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag && !flag;
    }
}
