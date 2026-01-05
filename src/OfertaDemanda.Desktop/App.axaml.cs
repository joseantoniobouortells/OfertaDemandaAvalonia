using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OfertaDemanda.Desktop.Controls;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Settings;
using OfertaDemanda.Desktop.ViewModels;
using OfertaDemanda.Shared.Math;

namespace OfertaDemanda.Desktop;

public partial class App : Application
{
    private const string AppDisplayName = "OfertaDemanda";
    private ThemeService? _themeService;
    private UserSettingsService? _userSettingsService;
    private LocalizationService? _localizationService;
    private MacMenuService? _macMenuService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Name = AppDisplayName;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settingsStore = new UserSettingsStore();
        _userSettingsService = new UserSettingsService(settingsStore);
        _themeService = new ThemeService(this, _userSettingsService);
        _themeService.Initialize();
        _localizationService = new LocalizationService(_userSettingsService);
        MathBlock.DefaultRenderer = new CSharpMathFormulaRenderer();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var aboutNavigator = new AboutNavigator();
            _macMenuService = new MacMenuService(_localizationService, aboutNavigator);
            _macMenuService.Initialize(this);

            var mainWindow = new MainWindow();
            mainWindow.AttachAboutNavigator(aboutNavigator);
            mainWindow.DataContext = new MainViewModel(_themeService, _userSettingsService, _localizationService);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
