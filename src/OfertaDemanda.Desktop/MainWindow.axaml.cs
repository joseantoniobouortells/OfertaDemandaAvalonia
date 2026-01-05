using System;
using Avalonia.Controls;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Desktop.ViewModels;

namespace OfertaDemanda.Desktop;

public partial class MainWindow : Window
{
    private const int AboutTabIndex = 3;
    private IAboutNavigator? _aboutNavigator;

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

    private void OnAboutRequested(object? sender, EventArgs e)
    {
        ShowAboutTab();
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
}
