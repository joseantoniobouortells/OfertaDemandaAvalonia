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
    private ThemeService? _themeService;
    private UserSettingsService? _userSettingsService;
    private LocalizationService? _localizationService;
    private MacMenuService? _macMenuService;

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
        UpdateAppName();
        MathBlock.DefaultRenderer = new CSharpMathFormulaRenderer();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var aboutNavigator = new AboutNavigator();
            var settingsNavigator = new SettingsNavigator();
            _macMenuService = new MacMenuService(_localizationService, aboutNavigator, settingsNavigator);
            _macMenuService.Initialize(this);
            _localizationService.CultureChanged += (_, _) =>
            {
                UpdateAppName();
                _macMenuService.RebuildMenu();
            };

            var mainWindow = new MainWindow();
            mainWindow.AttachAboutNavigator(aboutNavigator);
            mainWindow.AttachSettingsNavigator(settingsNavigator);
            mainWindow.DataContext = new MainViewModel(_themeService, _userSettingsService, _localizationService);
            _macMenuService.AttachToWindow(mainWindow);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void UpdateAppName()
    {
        if (_localizationService == null)
        {
            return;
        }

        Name = _localizationService["AppName"];
    }
}
