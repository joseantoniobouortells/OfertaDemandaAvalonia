using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile;

public partial class AppShell : Shell
{
    private bool _isInitializing;

    public AppShell()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext != null || _isInitializing)
        {
            return;
        }

        _isInitializing = true;
        _ = LoadViewModelAsync();
    }

    private async Task LoadViewModelAsync()
    {
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null)
            {
                return;
            }

            var viewModel = await Task.Run(() => services.GetService<MainViewModel>());
            if (viewModel != null)
            {
                Dispatcher.Dispatch(() => BindingContext = viewModel);
            }
        }
        finally
        {
            _isInitializing = false;
        }
    }
}
