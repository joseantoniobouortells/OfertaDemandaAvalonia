using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OfertaDemanda.Core.Expressions;

namespace OfertaDemanda.Desktop.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    protected bool TryParseExpression(string raw, string label, List<string> errors, out ParsedExpression? expression)
    {
        try
        {
            expression = ExpressionParser.Parse(raw);
            return true;
        }
        catch (ExpressionParseException ex)
        {
            errors.Add($"{label}: {ex.Message}");
            expression = null;
            return false;
        }
    }

    protected static string FormatMetric(string label, double? value, string format = "F2")
    {
        var suffix = value.HasValue
            ? value.Value.ToString(format, CultureInfo.InvariantCulture)
            : "—";
        return $"{label}: {suffix}";
    }
}
