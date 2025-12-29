using Avalonia;
using Avalonia.Styling;

namespace OfertaDemanda.Desktop.Services;

public sealed class ThemeService
{
    private readonly Application _application;
    private readonly ThemeSettingsStore _store;

    public ThemeService(Application application, ThemeSettingsStore store)
    {
        _application = application;
        _store = store;
    }

    public ThemeMode CurrentMode { get; private set; } = ThemeMode.System;

    public string SettingsFilePath => _store.SettingsFilePath;

    public void Initialize()
    {
        var storedMode = _store.Load();
        Apply(storedMode, persist: false);
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

        if (persist)
        {
            _store.Save(mode);
        }
    }
}
