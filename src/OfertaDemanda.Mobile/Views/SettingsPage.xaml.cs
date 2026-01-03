using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetService<SettingsViewModel>();
    }
}
