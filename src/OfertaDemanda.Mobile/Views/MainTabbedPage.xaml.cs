using System;
using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class MainTabbedPage : TabbedPage
{
    public MainTabbedPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MainTabbedPage InitializeComponent failed: {ex}");
            throw;
        }
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            BindingContext ??= Application.Current?.Handler?.MauiContext?.Services.GetService<MainViewModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MainTabbedPage OnAppearing failed: {ex}");
            throw;
        }
    }
}
