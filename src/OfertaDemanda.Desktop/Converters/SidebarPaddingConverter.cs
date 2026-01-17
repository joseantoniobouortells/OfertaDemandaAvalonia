using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace OfertaDemanda.Desktop.Converters;

public sealed class SidebarPaddingConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isCollapsed && isCollapsed ? new Thickness(6) : new Thickness(12);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Thickness thickness && thickness.Left <= 6;
    }
}
