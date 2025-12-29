using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OfertaDemanda.Desktop.Services;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService;

    public SettingsViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        ThemeOptions = new[]
        {
            new SelectionOption<ThemeMode>("Predeterminado del sistema", ThemeMode.System),
            new SelectionOption<ThemeMode>("Tema claro", ThemeMode.Light),
            new SelectionOption<ThemeMode>("Tema oscuro", ThemeMode.Dark)
        };

        SelectedTheme = ThemeOptions.First(o => o.Value == _themeService.CurrentMode);
    }

    public IReadOnlyList<SelectionOption<ThemeMode>> ThemeOptions { get; }

    public string SettingsFilePath => _themeService.SettingsFilePath;

    [ObservableProperty]
    private SelectionOption<ThemeMode> selectedTheme = null!;

    partial void OnSelectedThemeChanged(SelectionOption<ThemeMode> value)
    {
        if (value != null)
        {
            _themeService.Apply(value.Value);
        }
    }
}
