using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            BindingContext ??= Application.Current?.Handler?.MauiContext?.Services.GetService<AboutViewModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AboutPage OnAppearing failed: {ex}");
            throw;
        }
    }
}
