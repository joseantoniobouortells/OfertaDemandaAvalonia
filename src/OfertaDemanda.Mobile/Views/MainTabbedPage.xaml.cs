using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class MainTabbedPage : TabbedPage
{
    public MainTabbedPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetService<MainViewModel>();
    }
}
