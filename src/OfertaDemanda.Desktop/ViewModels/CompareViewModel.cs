using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Desktop.Services;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class CompareViewModel : ViewModelBase
{
    private const double MaxQuantity = 100d;
    private readonly MarketViewModel _market;
    private readonly FirmViewModel _firm;
    private MarketResult? _marketResult;
    private FirmResult? _firmResult;

    [ObservableProperty]
    private bool showMarketDemand = true;

    [ObservableProperty]
    private bool showMarketSupply = true;

    [ObservableProperty]
    private bool showFirmMarginalCost = true;

    [ObservableProperty]
    private bool showFirmAverageCost = true;

    [ObservableProperty]
    private bool showFirmAverageVariableCost = true;

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "Q", MinLimit = 0, MaxLimit = MaxQuantity }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "P / Costes", MinLimit = 0, MaxLimit = 150 }
    };

    public CompareViewModel(MarketViewModel market, FirmViewModel firm, LocalizationService localization)
        : base(localization)
    {
        _market = market;
        _firm = firm;
        _market.PropertyChanged += OnSourcePropertyChanged;
        _firm.PropertyChanged += OnSourcePropertyChanged;
        Localization.CultureChanged += (_, _) => OnLocalizationChanged();
        UpdateAxisLabels();
        Recalculate();
    }

    partial void OnShowMarketDemandChanged(bool value) => UpdateSeries();
    partial void OnShowMarketSupplyChanged(bool value) => UpdateSeries();
    partial void OnShowFirmMarginalCostChanged(bool value) => UpdateSeries();
    partial void OnShowFirmAverageCostChanged(bool value) => UpdateSeries();
    partial void OnShowFirmAverageVariableCostChanged(bool value) => UpdateSeries();
    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarketViewModel.DemandExpression)
            or nameof(MarketViewModel.SupplyExpression)
            or nameof(MarketViewModel.DemandShock)
            or nameof(MarketViewModel.SupplyShock)
            or nameof(MarketViewModel.Tax)
            or nameof(FirmViewModel.CostExpression)
            or nameof(FirmViewModel.Price)
            or nameof(FirmViewModel.SelectedMode))
        {
            Recalculate();
        }
    }

    private void Recalculate()
    {
        var localErrors = new List<string>();
        MarketResult? marketResult = null;
        FirmResult? firmResult = null;

        if (TryParseExpression(_market.DemandExpression, Localization["Compare_Parse_MarketDemand"], localErrors, out var demand) &&
            TryParseExpression(_market.SupplyExpression, Localization["Compare_Parse_MarketSupply"], localErrors, out var supply))
        {
            marketResult = MarketCalculator.Calculate(new MarketParameters(
                demand!,
                supply!,
                _market.DemandShock,
                _market.SupplyShock,
                _market.Tax));
            if (marketResult.Errors.Count > 0)
            {
                localErrors.AddRange(marketResult.Errors);
            }
        }

        if (TryParseExpression(_firm.CostExpression, Localization["Compare_Parse_FirmCost"], localErrors, out var cost))
        {
            firmResult = FirmCalculator.Calculate(new FirmParameters(cost!, _firm.Price, _firm.SelectedMode.Value));
            if (firmResult.Errors.Count > 0)
            {
                localErrors.AddRange(firmResult.Errors);
            }
        }

        _marketResult = marketResult;
        _firmResult = firmResult;
        UpdateSeries();
        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    private void UpdateSeries()
    {
        var list = new List<ISeries>();

        if (_marketResult != null)
        {
            if (ShowMarketDemand)
            {
                list.Add(ChartSeriesBuilder.Line(
                    Localization["Compare_Series_MarketDemand"],
                    _marketResult.DemandShifted,
                    SKColors.SteelBlue));
            }

            if (ShowMarketSupply)
            {
                list.Add(ChartSeriesBuilder.Line(
                    Localization["Compare_Series_MarketSupply"],
                    _marketResult.SupplyShifted,
                    SKColors.OliveDrab));
            }
        }

        if (_firmResult != null)
        {
            if (ShowFirmMarginalCost)
            {
                list.Add(ChartSeriesBuilder.Line(
                    Localization["Compare_Series_FirmMarginalCost"],
                    _firmResult.MarginalCost,
                    SKColors.MediumPurple));
            }

            if (ShowFirmAverageCost)
            {
                list.Add(ChartSeriesBuilder.Line(
                    Localization["Compare_Series_FirmAverageCost"],
                    _firmResult.AverageCost,
                    SKColors.DarkOrange));
            }

            if (ShowFirmAverageVariableCost)
            {
                list.Add(ChartSeriesBuilder.Line(
                    Localization["Compare_Series_FirmAverageVariableCost"],
                    _firmResult.AverageVariableCost,
                    SKColors.DeepSkyBlue));
            }
        }

        Series = list;
    }

    private void UpdateAxisLabels()
    {
        if (XAxes.Length > 0)
        {
            XAxes[0].Name = Localization["Axis_Quantity"];
        }

        if (YAxes.Length > 0)
        {
            YAxes[0].Name = Localization["Compare_Axis_PriceCosts"];
        }
    }

    private void OnLocalizationChanged()
    {
        UpdateAxisLabels();
        UpdateSeries();
    }
}
