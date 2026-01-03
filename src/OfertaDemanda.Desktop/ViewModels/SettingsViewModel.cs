using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OfertaDemanda.Desktop.Services;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService;
    private readonly LocalizationService _localization;

    public SettingsViewModel(ThemeService themeService, LocalizationService localization)
    {
        _themeService = themeService;
        _localization = localization;
        _localization.CultureChanged += (_, _) => OnLocalizationChanged();
        ThemeOptions = BuildThemeOptions();
        LanguageOptions = BuildLanguageOptions();
        SelectedTheme = ThemeOptions.First(o => o.Value == _themeService.CurrentMode);
        SelectedLanguage = LanguageOptions.First(o => string.Equals(o.Code, _localization.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase));
    }

    public LocalizationService Localization => _localization;

    [ObservableProperty]
    private IReadOnlyList<SelectionOption<ThemeMode>> themeOptions = Array.Empty<SelectionOption<ThemeMode>>();

    [ObservableProperty]
    private ObservableCollection<LanguageOption> languageOptions = new();

    public string SettingsFilePath => _themeService.SettingsFilePath;

    [ObservableProperty]
    private SelectionOption<ThemeMode> selectedTheme = null!;

    [ObservableProperty]
    private LanguageOption? selectedLanguage;

    partial void OnSelectedThemeChanged(SelectionOption<ThemeMode> value)
    {
        if (value != null)
        {
            _themeService.Apply(value.Value);
        }
    }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value != null)
        {
            _localization.ApplyCulture(value.Code);
        }
    }

    private IReadOnlyList<SelectionOption<ThemeMode>> BuildThemeOptions()
    {
        return new[]
        {
            new SelectionOption<ThemeMode>(_localization["Settings_Theme_System"], ThemeMode.System),
            new SelectionOption<ThemeMode>(_localization["Settings_Theme_Light"], ThemeMode.Light),
            new SelectionOption<ThemeMode>(_localization["Settings_Theme_Dark"], ThemeMode.Dark)
        };
    }

    private ObservableCollection<LanguageOption> BuildLanguageOptions()
    {
        return new ObservableCollection<LanguageOption>
        {
            new("es-ES", _localization["Language_Spanish"]),
            new("en-US", _localization["Language_English"]),
            new("fr-FR", _localization["Language_French"]),
            new("it-IT", _localization["Language_Italian"])
        };
    }

    private void OnLocalizationChanged()
    {
        ThemeOptions = BuildThemeOptions();
        UpdateLanguageLabels();
        SelectedTheme = ThemeOptions.First(o => o.Value == _themeService.CurrentMode);
        SelectedLanguage = LanguageOptions.First(o => string.Equals(o.Code, _localization.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateLanguageLabels()
    {
        foreach (var option in LanguageOptions)
        {
            option.Label = option.Code switch
            {
                "es-ES" => _localization["Language_Spanish"],
                "en-US" => _localization["Language_English"],
                "fr-FR" => _localization["Language_French"],
                "it-IT" => _localization["Language_Italian"],
                _ => option.Label
            };
        }
    }
}

public sealed partial class LanguageOption : ObservableObject
{
    public LanguageOption(string code, string label)
    {
        Code = code;
        this.label = label;
    }

    public string Code { get; }

    [ObservableProperty]
    private string label;
}
