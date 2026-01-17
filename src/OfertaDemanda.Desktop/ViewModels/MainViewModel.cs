using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool showIsoBenefitPanel;

    [ObservableProperty]
    private string appTitle = string.Empty;

    public LocalizationService Localization { get; }
    public MarketViewModel Market { get; }
    public FirmViewModel Firm { get; }
    public MonopolyViewModel Monopoly { get; }
    public CompareViewModel Compare { get; }
    public ElasticityViewModel Elasticity { get; }
    public SettingsViewModel Settings { get; }
    public IsoBenefitViewModel IsoBenefit { get; }
    public AboutViewModel About { get; }
    public IRelayCommand ResetDefaultsCommand { get; }
    public string AppVersion { get; }

    public MainViewModel(ThemeService themeService, UserSettingsService userSettingsService, LocalizationService localizationService)
    {
        Localization = localizationService;
        ShowIsoBenefitPanel = false;
        Market = new MarketViewModel(localizationService);
        Firm = new FirmViewModel(localizationService);
        Monopoly = new MonopolyViewModel(localizationService);
        Compare = new CompareViewModel(Market, Firm, localizationService);
        Elasticity = new ElasticityViewModel(Market, localizationService);
        IsoBenefit = new IsoBenefitViewModel(userSettingsService, localizationService);
        Settings = new SettingsViewModel(themeService, localizationService);
        About = new AboutViewModel(localizationService);
        ResetDefaultsCommand = new RelayCommand(ApplyDefaults);
        AppVersion = ResolveVersion(Assembly.GetEntryAssembly() ?? typeof(MainViewModel).Assembly);
        Localization.CultureChanged += (_, _) => UpdateAppTitle();
        UpdateAppTitle();
        ApplyDefaults();
    }

    public bool ShowFirmChart => !ShowIsoBenefitPanel;

    partial void OnShowIsoBenefitPanelChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowFirmChart));
    }

    private void ApplyDefaults()
    {
        Market.ApplyDefaults();
        Firm.ApplyDefaults();
        Monopoly.ApplyDefaults();
        Elasticity.ApplyDefaults();
    }

    private void UpdateAppTitle()
    {
        AppTitle = $"{Localization["App_Title"]} {AppVersion}";
    }

    private static string ResolveVersion(Assembly assembly)
    {
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var trimmed = info.Split('+', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            return trimmed;
        }

        return assembly.GetName().Version?.ToString() ?? "—";
    }
}
