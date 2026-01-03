using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class MonopolyPage : TabbedPage
{
    public MonopolyPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetService<MainViewModel>();
    }
}
