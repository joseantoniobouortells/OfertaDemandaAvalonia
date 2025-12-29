using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfertaDemanda.Desktop.Services;

namespace OfertaDemanda.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public MarketViewModel Market { get; }
    public FirmViewModel Firm { get; }
    public MonopolyViewModel Monopoly { get; }
    public ElasticityViewModel Elasticity { get; }
    public SettingsViewModel Settings { get; }
    public IsoBenefitViewModel IsoBenefit { get; }
    public IRelayCommand ResetDefaultsCommand { get; }

    public MainViewModel(ThemeService themeService, UserSettingsService userSettingsService)
    {
        Market = new MarketViewModel();
        Firm = new FirmViewModel();
        Monopoly = new MonopolyViewModel();
        Elasticity = new ElasticityViewModel(Market);
        IsoBenefit = new IsoBenefitViewModel(userSettingsService);
        Settings = new SettingsViewModel(themeService);
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
