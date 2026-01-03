using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Collections.Generic;
using System.Linq;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Mobile.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly CultureInfo FallbackCulture = new(AppLocalization.DefaultCultureCode);
    private static readonly IReadOnlyList<CultureInfo> SupportedCultures =
        AppLocalization.SupportedCultureCodes.Select(code => new CultureInfo(code)).ToArray();

    private readonly UserSettingsService _settingsService;
    private readonly ResourceManager _resourceManager =
        new("OfertaDemanda.Mobile.Resources.Strings", typeof(LocalizationService).Assembly);

    public LocalizationService(UserSettingsService settingsService)
    {
        _settingsService = settingsService;
        CurrentCulture = SupportedCultures[0];
        ApplyCulture(settingsService.Settings.Language, persist: false);
    }

    public CultureInfo CurrentCulture { get; private set; }

    public event EventHandler? CultureChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => GetString(key);

    public static IReadOnlyList<CultureInfo> GetSupportedCultures() => SupportedCultures;

    public void ApplyCulture(string? code, bool persist = true)
    {
        var culture = AppLocalization.ResolveCulture(code);
        ApplyCulture(culture, persist);
    }

    public void ApplyCulture(CultureInfo culture, bool persist = true)
    {
        if (Equals(CurrentCulture, culture) &&
            Equals(CultureInfo.CurrentCulture, culture) &&
            Equals(CultureInfo.CurrentUICulture, culture))
        {
            return;
        }

        CurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (persist && _settingsService.Settings.Language != culture.Name)
        {
            var updated = _settingsService.Settings with { Language = culture.Name };
            _settingsService.Update(updated);
        }

        CultureChanged?.Invoke(this, EventArgs.Empty);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
    }

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var value = _resourceManager.GetString(key, CurrentCulture);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = _resourceManager.GetString(key, FallbackCulture);
        }

        return string.IsNullOrWhiteSpace(value) ? $"!{key}!" : value;
    }
}
