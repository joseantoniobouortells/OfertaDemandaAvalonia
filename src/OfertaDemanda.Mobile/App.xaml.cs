using System;
using OfertaDemanda.Mobile.Services;
using OfertaDemanda.Mobile.Views;

namespace OfertaDemanda.Mobile;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App(IServiceProvider services, ThemeService themeService, LocalizationService localizationService, MainTabbedPage mainTabbedPage)
    {
        Services = services;
        InitializeComponent();

        themeService.Apply(themeService.CurrentMode, persist: false);
        localizationService.ApplyCulture(localizationService.CurrentCulture, persist: false);
        MainPage = mainTabbedPage;
    }
}
