using OfertaDemanda.Mobile.Services;
using OfertaDemanda.Mobile.Views;

namespace OfertaDemanda.Mobile;

public partial class App : Application
{
    public App(ThemeService themeService, LocalizationService localizationService)
    {
        InitializeComponent();

        themeService.Apply(themeService.CurrentMode, persist: false);
        localizationService.ApplyCulture(localizationService.CurrentCulture, persist: false);
        MainPage = new MainTabbedPage();
    }
}
