using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Settings;

namespace OfertaDemanda.Desktop.ViewModels;

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
    public IRelayCommand ResetDefaultsCommand { get; }

    public MainViewModel(ThemeService themeService, UserSettingsService userSettingsService, LocalizationService localizationService)
    {
        Localization = localizationService;
        ShowIsoBenefitPanel = false;
        Market = new MarketViewModel(localizationService);
        Firm = new FirmViewModel(localizationService);
        Monopoly = new MonopolyViewModel(localizationService);
        Elasticity = new ElasticityViewModel(Market, localizationService);
        IsoBenefit = new IsoBenefitViewModel(userSettingsService, localizationService);
        Settings = new SettingsViewModel(themeService, localizationService);
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
