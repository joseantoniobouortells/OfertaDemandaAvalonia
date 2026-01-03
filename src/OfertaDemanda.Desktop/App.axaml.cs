using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Settings;
using OfertaDemanda.Desktop.ViewModels;

namespace OfertaDemanda.Desktop;

public partial class App : Application
{
    private ThemeService? _themeService;
    private UserSettingsService? _userSettingsService;
    private LocalizationService? _localizationService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settingsStore = new UserSettingsStore();
        _userSettingsService = new UserSettingsService(settingsStore);
        _themeService = new ThemeService(this, _userSettingsService);
        _themeService.Initialize();
        _localizationService = new LocalizationService(_userSettingsService);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(_themeService, _userSettingsService, _localizationService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
