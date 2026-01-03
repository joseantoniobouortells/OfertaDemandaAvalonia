using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfertaDemanda.Desktop.Services;

namespace OfertaDemanda.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
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

    private void ApplyDefaults()
    {
        Market.ApplyDefaults();
        Firm.ApplyDefaults();
        Monopoly.ApplyDefaults();
        Elasticity.ApplyDefaults();
    }
}
