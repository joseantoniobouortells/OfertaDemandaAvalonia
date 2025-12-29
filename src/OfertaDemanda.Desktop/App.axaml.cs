using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Desktop.ViewModels;

namespace OfertaDemanda.Desktop;

public partial class App : Application
{
    private ThemeService? _themeService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settingsStore = new ThemeSettingsStore();
        _themeService = new ThemeService(this, settingsStore);
        _themeService.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(_themeService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
