using Avalonia;
using Avalonia.Styling;

namespace OfertaDemanda.Desktop.Services;

public sealed class ThemeService
{
    private readonly Application _application;
    private readonly UserSettingsService _settingsService;

    public ThemeService(Application application, UserSettingsService settingsService)
    {
        _application = application;
        _settingsService = settingsService;
        CurrentMode = settingsService.Settings.Theme;
    }

    public ThemeMode CurrentMode { get; private set; } = ThemeMode.System;

    public string SettingsFilePath => _settingsService.SettingsFilePath;

    public void Initialize()
    {
        Apply(_settingsService.Settings.Theme, persist: false);
    }

    public void Apply(ThemeMode mode, bool persist = true)
    {
        CurrentMode = mode;
        _application.RequestedThemeVariant = mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        if (persist && _settingsService.Settings.Theme != mode)
        {
            var updated = _settingsService.Settings with { Theme = mode };
            _settingsService.Update(updated);
        }
    }
}
