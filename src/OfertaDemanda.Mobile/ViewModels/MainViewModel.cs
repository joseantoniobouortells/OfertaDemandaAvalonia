using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfertaDemanda.Mobile.Services;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Mobile.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool showIsoBenefitPanel;

    public LocalizationService Localization { get; }
    public MarketViewModel Market { get; }
    public FirmViewModel Firm { get; }
    public MonopolyViewModel Monopoly { get; }
    public ElasticityViewModel Elasticity { get; }
    public SettingsViewModel Settings { get; }
    public IsoBenefitViewModel IsoBenefit { get; }
    public AboutViewModel About { get; }
    public ThemeService Theme { get; }
    public IRelayCommand ResetDefaultsCommand { get; }

    public MainViewModel(ThemeService themeService, UserSettingsService userSettingsService, LocalizationService localizationService)
    {
        Localization = localizationService;
        Theme = themeService;
        ShowIsoBenefitPanel = false;
        Market = new MarketViewModel(localizationService);
        Firm = new FirmViewModel(localizationService);
        Monopoly = new MonopolyViewModel(localizationService);
        Elasticity = new ElasticityViewModel(Market, localizationService);
        IsoBenefit = new IsoBenefitViewModel(userSettingsService, localizationService);
        Settings = new SettingsViewModel(themeService, localizationService, userSettingsService);
        About = new AboutViewModel(localizationService);
        ResetDefaultsCommand = new RelayCommand(ApplyDefaults);
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
}
