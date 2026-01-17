using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OfertaDemanda.Desktop.Converters;

public sealed class SidebarToggleIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isCollapsed && isCollapsed ? "»" : "«";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && text == "»";
    }
}
