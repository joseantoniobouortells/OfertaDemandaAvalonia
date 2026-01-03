using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile;

public partial class AppShell : Shell
{
    public AppShell(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
