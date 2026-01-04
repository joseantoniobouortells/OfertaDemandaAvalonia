using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            BindingContext ??= Application.Current?.Handler?.MauiContext?.Services.GetService<SettingsViewModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SettingsPage OnAppearing failed: {ex}");
            throw;
        }
    }
}
