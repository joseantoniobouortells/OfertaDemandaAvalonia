using System;
using Avalonia.Controls;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Desktop.ViewModels;

namespace OfertaDemanda.Desktop;

public partial class MainWindow : Window
{
    private const int SettingsTabIndex = 2;
    private const int AboutTabIndex = 3;
    private IAboutNavigator? _aboutNavigator;
    private ISettingsNavigator? _settingsNavigator;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void AttachAboutNavigator(IAboutNavigator navigator)
    {
        if (_aboutNavigator != null)
        {
            _aboutNavigator.Requested -= OnAboutRequested;
        }

        _aboutNavigator = navigator;
        _aboutNavigator.Requested += OnAboutRequested;
    }

    public void AttachSettingsNavigator(ISettingsNavigator navigator)
    {
        if (_settingsNavigator != null)
        {
            _settingsNavigator.Requested -= OnSettingsRequested;
        }

        _settingsNavigator = navigator;
        _settingsNavigator.Requested += OnSettingsRequested;
    }

    private void OnAboutRequested(object? sender, EventArgs e)
    {
        ShowAboutTab();
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        ShowSettingsTab();
    }

    private void ShowAboutTab()
    {
        if (MainTabs == null)
        {
            return;
        }

        MainTabs.SelectedIndex = AboutTabIndex;
        Activate();
    }

    private void ShowSettingsTab()
    {
        if (MainTabs == null)
        {
            return;
        }

        MainTabs.SelectedIndex = SettingsTabIndex;
        Activate();
    }
}
