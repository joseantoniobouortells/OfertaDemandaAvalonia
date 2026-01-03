using System.Collections.Generic;
using System.Linq;

namespace OfertaDemanda.Shared.Settings;

public sealed record IsoBenefitFirmSetting(string Name, string CostExpression)
{
    public static IsoBenefitFirmSetting Create(string name, string costExpression) =>
        new(name, string.IsNullOrWhiteSpace(costExpression) ? AppDefaults.Firm.CostExpression : costExpression);
}

public sealed record IsoBenefitSettings
{
    public string DemandExpression { get; init; } = AppDefaults.Market.DemandExpression;
    public double DemandShock { get; init; } = AppDefaults.Market.DemandShock;
    public IReadOnlyList<IsoBenefitFirmSetting> Firms { get; init; } = DefaultFirms();

    public static IsoBenefitSettings CreateDefault() => new()
    {
        DemandExpression = AppDefaults.Market.DemandExpression,
        DemandShock = AppDefaults.Market.DemandShock,
        Firms = DefaultFirms()
    };

    private static IReadOnlyList<IsoBenefitFirmSetting> DefaultFirms() =>
        [
            new IsoBenefitFirmSetting("Empresa A", "200 + 10q + 0.5q^2"),
            new IsoBenefitFirmSetting("Empresa B", "120 + 12q + 0.3q^2"),
            new IsoBenefitFirmSetting("Empresa C", "80 + 8q + 0.8q^2")
        ];

    public IsoBenefitSettings Sanitize()
    {
        var firms = (Firms == null || Firms.Count == 0 ? DefaultFirms() : Firms)
            .Where(f => !string.IsNullOrWhiteSpace(f.CostExpression))
            .Select((f, idx) =>
            {
                var name = string.IsNullOrWhiteSpace(f.Name) ? $"Empresa {(char)('A' + idx)}" : f.Name;
                return new IsoBenefitFirmSetting(name.Trim(), f.CostExpression.Trim());
            })
            .ToArray();

        return this with
        {
            DemandExpression = string.IsNullOrWhiteSpace(DemandExpression)
                ? AppDefaults.Market.DemandExpression
                : DemandExpression,
            Firms = firms
        };
    }
}

public sealed record UserSettings
{
    public ThemeMode Theme { get; init; } = ThemeMode.System;
    public string Language { get; init; } = AppLocalization.DefaultCultureCode;
    public IsoBenefitSettings IsoBenefit { get; init; } = IsoBenefitSettings.CreateDefault();

    public static UserSettings CreateDefault() => new()
    {
        Theme = ThemeMode.System,
        Language = AppLocalization.DefaultCultureCode,
        IsoBenefit = IsoBenefitSettings.CreateDefault()
    };

    public UserSettings Sanitize()
    {
        var iso = IsoBenefit ?? IsoBenefitSettings.CreateDefault();
        return this with
        {
            Language = AppLocalization.NormalizeCultureCode(Language),
            IsoBenefit = iso.Sanitize()
        };
    }
}
