using System;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Mobile.Services;

public sealed class ThemeService
{
    private readonly UserSettingsService _settingsService;

    public ThemeService(UserSettingsService settingsService)
    {
        _settingsService = settingsService;
        CurrentMode = settingsService.Settings.Theme;
        Apply(settingsService.Settings.Theme, persist: false);
    }

    public ThemeMode CurrentMode { get; private set; } = ThemeMode.System;

    public event EventHandler? ThemeChanged;

    public void Apply(ThemeMode mode, bool persist = true)
    {
        CurrentMode = mode;
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = mode switch
            {
                ThemeMode.Light => AppTheme.Light,
                ThemeMode.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }

        if (persist && _settingsService.Settings.Theme != mode)
        {
            var updated = _settingsService.Settings with { Theme = mode };
            _settingsService.Update(updated);
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
