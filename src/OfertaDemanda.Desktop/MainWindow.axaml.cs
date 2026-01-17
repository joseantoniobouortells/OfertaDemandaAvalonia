using System;
using System.Linq;
using Avalonia.Controls;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Desktop.ViewModels;

namespace OfertaDemanda.Desktop;

public partial class MainWindow : Window
{
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
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedNavigationItem = viewModel.NavigationItems.FirstOrDefault(item => item is AboutNavigationItem)
                                               ?? viewModel.SelectedNavigationItem;
        }

        Activate();
    }

    private void ShowSettingsTab()
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedNavigationItem = viewModel.NavigationItems.FirstOrDefault(item => item is SettingsNavigationItem)
                                               ?? viewModel.SelectedNavigationItem;
        }

        Activate();
    }
}
