using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace OfertaDemanda.Desktop.Services;

public sealed class MacMenuService
{
    private readonly LocalizationService _localization;
    private readonly IAboutNavigator _aboutNavigator;
    private NativeMenuItem? _appMenuItem;
    private NativeMenuItem? _aboutMenuItem;
    private NativeMenuItem? _hideMenuItem;
    private NativeMenuItem? _hideOthersMenuItem;
    private NativeMenuItem? _showAllMenuItem;
    private NativeMenuItem? _quitMenuItem;

    public MacMenuService(LocalizationService localization, IAboutNavigator aboutNavigator)
    {
        _localization = localization;
        _aboutNavigator = aboutNavigator;
    }

    public void Initialize(Application app)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        BuildMenu(app);
        _localization.CultureChanged += (_, _) => UpdateMenuHeaders();
    }

    private void BuildMenu(Application app)
    {
        _appMenuItem = new NativeMenuItem();
        _aboutMenuItem = new NativeMenuItem();
        _aboutMenuItem.Click += (_, _) => _aboutNavigator.ShowAbout();

        _hideMenuItem = new NativeMenuItem();
        _hideMenuItem.Click += (_, _) => HideAppWindows(app);

        _hideOthersMenuItem = new NativeMenuItem();
        _hideOthersMenuItem.Click += (_, _) => HideOtherWindows(app);

        _showAllMenuItem = new NativeMenuItem();
        _showAllMenuItem.Click += (_, _) => ShowAllWindows(app);

        _quitMenuItem = new NativeMenuItem();
        _quitMenuItem.Click += (_, _) =>
        {
            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };

        var appSubmenu = new NativeMenu();
        appSubmenu.Items.Add(_aboutMenuItem);
        appSubmenu.Items.Add(new NativeMenuItemSeparator());
        appSubmenu.Items.Add(_hideMenuItem);
        appSubmenu.Items.Add(_hideOthersMenuItem);
        appSubmenu.Items.Add(_showAllMenuItem);
        appSubmenu.Items.Add(new NativeMenuItemSeparator());
        appSubmenu.Items.Add(_quitMenuItem);

        _appMenuItem.Menu = appSubmenu;
        var appMenu = new NativeMenu();
        appMenu.Items.Add(_appMenuItem);

        NativeMenu.SetMenu(app, appMenu);
        UpdateMenuHeaders();
    }

    private void UpdateMenuHeaders()
    {
        if (_appMenuItem == null ||
            _aboutMenuItem == null ||
            _hideMenuItem == null ||
            _hideOthersMenuItem == null ||
            _showAllMenuItem == null ||
            _quitMenuItem == null)
        {
            return;
        }

        var appName = _localization["App_Title"];
        _appMenuItem.Header = appName;
        _aboutMenuItem.Header = string.Format(_localization.CurrentCulture, _localization["Menu_About"], appName);
        _hideMenuItem.Header = string.Format(_localization.CurrentCulture, _localization["Menu_Hide"], appName);
        _hideOthersMenuItem.Header = _localization["Menu_HideOthers"];
        _showAllMenuItem.Header = _localization["Menu_ShowAll"];
        _quitMenuItem.Header = string.Format(_localization.CurrentCulture, _localization["Menu_Quit"], appName);
    }

    private static void HideAppWindows(Application app)
    {
        if (app.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows)
        {
            window.Hide();
        }
    }

    private static void HideOtherWindows(Application app)
    {
        if (app.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var mainWindow = desktop.MainWindow;
        foreach (var window in desktop.Windows)
        {
            if (!ReferenceEquals(window, mainWindow))
            {
                window.Hide();
            }
        }
    }

    private static void ShowAllWindows(Application app)
    {
        if (app.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows)
        {
            window.Show();
        }

        desktop.MainWindow?.Activate();
    }
}
