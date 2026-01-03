using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class FirmPage : ContentPage
{
    public FirmPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetService<MainViewModel>();
    }
}
