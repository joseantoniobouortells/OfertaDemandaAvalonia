using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OfertaDemanda.Shared.Settings;

public static class AppLocalization
{
    public const string DefaultCultureCode = "es-ES";

    public static readonly IReadOnlyList<string> SupportedCultureCodes =
    [
        "es-ES",
        "en-US",
        "fr-FR",
        "it-IT",
        "de-DE"
    ];

    public static string NormalizeCultureCode(string? code)
    {
        return ResolveCulture(code).Name;
    }

    public static CultureInfo ResolveCulture(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new CultureInfo(DefaultCultureCode);
        }

        var trimmed = code.Trim();
        var exact = SupportedCultureCodes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return new CultureInfo(exact);
        }

        var neutral = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
        var byNeutral = SupportedCultureCodes.FirstOrDefault(c => string.Equals(new CultureInfo(c).TwoLetterISOLanguageName, neutral, StringComparison.OrdinalIgnoreCase));
        return new CultureInfo(byNeutral ?? DefaultCultureCode);
    }
}
