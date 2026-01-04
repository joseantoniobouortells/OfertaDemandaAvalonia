using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

namespace OfertaDemanda.Mobile.Views;

public partial class PerfectCompetitionPage : ContentPage
{
    private MainViewModel? _viewModel;
    private View? _marketView;
    private View? _firmView;
    private View? _elasticityView;
    private bool _isLocalizationHooked;

    public PerfectCompetitionPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PerfectCompetitionPage InitializeComponent failed: {ex}");
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
            Console.WriteLine($"PerfectCompetitionPage OnAppearing failed: {ex}");
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

        _marketView ??= new MarketPage { BindingContext = _viewModel.Market };
        _firmView ??= new FirmPage { BindingContext = _viewModel };
        _elasticityView ??= new ElasticityPage { BindingContext = _viewModel.Elasticity };
    }

    private void UpdateSectionLabels()
    {
        if (_viewModel == null)
        {
            return;
        }

        var labels = new List<string>
        {
            _viewModel.Localization["Tab_Market"],
            _viewModel.Localization["Tab_Firm"],
            _viewModel.Localization["Tab_Elasticity"]
        };

        SectionPicker.ItemsSource = labels;
    }

    private void SwitchSection(int selectedIndex)
    {
        EnsureViews();
        SectionHost.Content = selectedIndex switch
        {
            0 => _marketView,
            1 => _firmView,
            2 => _elasticityView,
            _ => _marketView
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
