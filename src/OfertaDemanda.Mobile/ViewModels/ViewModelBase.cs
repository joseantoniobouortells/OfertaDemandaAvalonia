using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Mobile.Services;

namespace OfertaDemanda.Mobile.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    protected ViewModelBase(LocalizationService localization)
    {
        Localization = localization;
    }

    public LocalizationService Localization { get; }

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

    protected string FormatMetric(string labelKey, double? value, string format = "F2")
    {
        return FormatMetricLabel(Localization[labelKey], value, format);
    }

    protected string FormatMetricLabel(string label, double? value, string format = "F2")
    {
        var suffix = value.HasValue
            ? value.Value.ToString(format, Localization.CurrentCulture)
            : Localization["Common_EmptyValue"];

        return string.Format(Localization.CurrentCulture, Localization["Format_LabelValue"], label, suffix);
    }

    protected string FormatLabelValue(string label, string? value)
    {
        var suffix = string.IsNullOrWhiteSpace(value) ? Localization["Common_EmptyValue"] : value;
        return string.Format(Localization.CurrentCulture, Localization["Format_LabelValue"], label, suffix);
    }
}
