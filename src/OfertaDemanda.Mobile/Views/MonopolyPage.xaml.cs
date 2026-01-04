using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class MonopolyPage : ContentPage
{
    private MainViewModel? _viewModel;
    private View? _modelView;
    private View? _welfareView;
    private bool _isLocalizationHooked;

    public MonopolyPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MonopolyPage InitializeComponent failed: {ex}");
            throw;
        }
        SectionPicker.SelectedIndexChanged += (_, _) => SwitchSection(SectionPicker.SelectedIndex);
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            _viewModel ??= Application.Current?.Handler?.MauiContext?.Services.GetService<MainViewModel>();
            if (_viewModel == null)
            {
                return;
            }

            BindingContext ??= _viewModel;
            EnsureViews();
            UpdateSectionLabels();

            if (SectionPicker.SelectedIndex < 0)
            {
                SectionPicker.SelectedIndex = 0;
            }

            if (!_isLocalizationHooked)
            {
                _viewModel.Localization.CultureChanged += OnCultureChanged;
                _isLocalizationHooked = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MonopolyPage OnAppearing failed: {ex}");
            throw;
        }
    }

    protected override void OnDisappearing()
    {
        if (_isLocalizationHooked && _viewModel != null)
        {
            _viewModel.Localization.CultureChanged -= OnCultureChanged;
            _isLocalizationHooked = false;
        }

        base.OnDisappearing();
    }

    private void EnsureViews()
    {
        if (_viewModel == null)
        {
            return;
        }

        _modelView ??= new MonopolyModelPage { BindingContext = _viewModel.Monopoly };
        _welfareView ??= new MonopolyWelfarePage { BindingContext = _viewModel.Monopoly };
    }

    private void UpdateSectionLabels()
    {
        if (_viewModel == null)
        {
            return;
        }

        var labels = new List<string>
        {
            _viewModel.Localization["Tab_Model"],
            _viewModel.Localization["Tab_Welfare"]
        };

        SectionPicker.ItemsSource = labels;
    }

    private void SwitchSection(int selectedIndex)
    {
        EnsureViews();
        SectionHost.Content = selectedIndex switch
        {
            0 => _modelView,
            1 => _welfareView,
            _ => _modelView
        };
    }

    private void OnCultureChanged(object? sender, System.EventArgs e)
    {
        var selectedIndex = SectionPicker.SelectedIndex;
        UpdateSectionLabels();
        if (selectedIndex >= 0)
        {
            SectionPicker.SelectedIndex = selectedIndex;
        }
    }
}
