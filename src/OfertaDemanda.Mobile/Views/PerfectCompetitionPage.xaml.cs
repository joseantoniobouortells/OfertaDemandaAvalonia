using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class PerfectCompetitionPage : TabbedPage
{
    public PerfectCompetitionPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetService<MainViewModel>();
    }
}
