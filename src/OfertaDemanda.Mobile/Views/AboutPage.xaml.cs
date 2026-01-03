using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetService<AboutViewModel>();
    }
}
