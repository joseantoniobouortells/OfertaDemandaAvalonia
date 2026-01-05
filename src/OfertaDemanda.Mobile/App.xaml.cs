using OfertaDemanda.Mobile.Controls;
using OfertaDemanda.Mobile.Services;
using OfertaDemanda.Shared.Math;
using OfertaDemanda.Mobile.Views;

namespace OfertaDemanda.Mobile;

public partial class App : Application
{
    public App(ThemeService themeService, LocalizationService localizationService, IMathFormulaRenderer mathRenderer)
    {
        InitializeComponent();

        themeService.Apply(themeService.CurrentMode, persist: false);
        localizationService.ApplyCulture(localizationService.CurrentCulture, persist: false);
        MathView.DefaultRenderer = mathRenderer;
        MainPage = new MainTabbedPage();
    }
}
