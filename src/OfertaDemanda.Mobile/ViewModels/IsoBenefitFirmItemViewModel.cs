using CommunityToolkit.Mvvm.ComponentModel;

namespace OfertaDemanda.Mobile.ViewModels;

public partial class IsoBenefitFirmItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string costExpression = string.Empty;
}
