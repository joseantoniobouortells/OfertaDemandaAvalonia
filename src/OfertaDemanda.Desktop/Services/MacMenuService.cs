using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;

namespace OfertaDemanda.Desktop.Services;

public sealed class MacMenuService
{
    private readonly LocalizationService _localization;
    private readonly IAboutNavigator _aboutNavigator;
    private readonly ISettingsNavigator _settingsNavigator;
    private Application? _app;
    private Window? _window;
    private NativeMenu? _menu;
    private NativeMenuItem? _appMenuItem;
    private NativeMenuItem? _aboutMenuItem;
    private NativeMenuItem? _preferencesMenuItem;
    private NativeMenuItem? _quitMenuItem;

    public MacMenuService(LocalizationService localization, IAboutNavigator aboutNavigator, ISettingsNavigator settingsNavigator)
    {
        _localization = localization;
        _aboutNavigator = aboutNavigator;
        _settingsNavigator = settingsNavigator;
    }

    public void Initialize(Application app)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        _app = app;
        BuildMenu(app);
        _localization.CultureChanged += (_, _) => UpdateMenuHeaders();
    }

    public void AttachToWindow(Window window)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        _window = window;
        if (_menu == null && _app != null)
        {
            BuildMenu(_app);
        }

        if (_menu != null)
        {
            NativeMenu.SetMenu(window, _menu);
        }
    }

    public void RebuildMenu()
    {
        if (_app == null || !OperatingSystem.IsMacOS())
        {
            return;
        }

        if (_menu == null)
        {
            BuildMenu(_app);
        }
        else
        {
            UpdateMenuHeaders();
        }
    }

    private void BuildMenu(Application app)
    {
        _appMenuItem = new NativeMenuItem();
        _aboutMenuItem = new NativeMenuItem();
        _aboutMenuItem.Click += (_, _) => _aboutNavigator.ShowAbout();

        _preferencesMenuItem = new NativeMenuItem();
        _preferencesMenuItem.Click += (_, _) => _settingsNavigator.ShowSettings();

        _quitMenuItem = new NativeMenuItem();
        _quitMenuItem.Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta);
        _quitMenuItem.Click += (_, _) =>
        {
            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };

        _menu = new NativeMenu();
        var appSubmenu = new NativeMenu();
        appSubmenu.Items.Add(_aboutMenuItem);
        appSubmenu.Items.Add(_preferencesMenuItem);
        appSubmenu.Items.Add(new NativeMenuItemSeparator());
        appSubmenu.Items.Add(_quitMenuItem);
        _appMenuItem.Menu = appSubmenu;
        _menu.Items.Add(_appMenuItem);

        NativeMenu.SetMenu(app, _menu);
        UpdateMenuHeaders();
    }

    private void UpdateMenuHeaders()
    {
        if (_appMenuItem == null ||
            _aboutMenuItem == null ||
            _preferencesMenuItem == null ||
            _quitMenuItem == null)
        {
            return;
        }

        var appName = _localization["AppName"];
        _appMenuItem.Header = appName;
        _aboutMenuItem.Header = string.Format(_localization.CurrentCulture, _localization["Menu_AboutFmt"], appName);
        _preferencesMenuItem.Header = _localization["Menu_Preferences"];
        _quitMenuItem.Header = string.Format(_localization.CurrentCulture, _localization["Menu_Quit"], appName);
    }
}
