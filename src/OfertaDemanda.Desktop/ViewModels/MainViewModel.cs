using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OfertaDemanda.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public MarketViewModel Market { get; }
    public FirmViewModel Firm { get; }
    public MonopolyViewModel Monopoly { get; }
    public ElasticityViewModel Elasticity { get; }
    public IRelayCommand ResetDefaultsCommand { get; }

    public MainViewModel()
    {
        Market = new MarketViewModel();
        Firm = new FirmViewModel();
        Monopoly = new MonopolyViewModel();
        Elasticity = new ElasticityViewModel(Market);
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
