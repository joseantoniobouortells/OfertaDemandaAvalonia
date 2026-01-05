using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Settings;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class FirmViewModel : ViewModelBase
{
    private const double FirmMaxQuantity = 60d;
    private bool _suppressUpdates;
    private SelectionOption<FirmMode>[] _modeOptions = Array.Empty<SelectionOption<FirmMode>>();

    [ObservableProperty]
    private string costExpression = "200 + 10q + 0.5q^2";

    [ObservableProperty]
    private double price = 40;

    [ObservableProperty]
    private SelectionOption<FirmMode> selectedMode = null!;

    [ObservableProperty]
    private bool showTotalRevenue;

    [ObservableProperty]
    private string currentPriceText = string.Empty;

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    [ObservableProperty]
    private string quantityText = string.Empty;

    [ObservableProperty]
    private string priceText = string.Empty;

    [ObservableProperty]
    private string profitText = string.Empty;

    public IReadOnlyList<SelectionOption<FirmMode>> ModeOptions => _modeOptions;

    public bool IsPriceEditable => SelectedMode.Value == FirmMode.ShortRun;

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "q", MinLimit = 0, MaxLimit = FirmMaxQuantity }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "Costes / P", MinLimit = 0, MaxLimit = 120 }
    };

    public FirmViewModel(LocalizationService localization)
        : base(localization)
    {
        _modeOptions = BuildModeOptions();
        Localization.CultureChanged += (_, _) => OnLocalizationChanged();
        UpdateAxisLabels();
        ApplyDefaults();
    }

    public void ApplyDefaults()
    {
        _suppressUpdates = true;
        CostExpression = AppDefaults.Firm.CostExpression;
        Price = AppDefaults.Firm.Price;
        SelectedMode = _modeOptions.First(o => o.Value == AppDefaults.Firm.Mode);
        ShowTotalRevenue = false;
        _suppressUpdates = false;
        Recalculate();
    }

    partial void OnCostExpressionChanged(string value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnPriceChanged(double value)
    {
        UpdateCurrentPriceText();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSelectedModeChanged(SelectionOption<FirmMode> value)
    {
        OnPropertyChanged(nameof(IsPriceEditable));
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowTotalRevenueChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(CostExpression, Localization["Firm_Parse_TotalCost"], localErrors, out var cost))
        {
            UpdateState(null, localErrors);
            return;
        }

        var result = FirmCalculator.Calculate(new FirmParameters(cost!, Price, SelectedMode.Value));
        if (result.Errors.Count > 0)
        {
            localErrors.AddRange(result.Errors);
        }

        UpdateState(result, localErrors);
    }

    private void UpdateState(FirmResult? result, List<string> localErrors)
    {
        if (result == null)
        {
            Series = Array.Empty<ISeries>();
            QuantityText = FormatMetric("Firm_Label_Quantity", null);
            PriceText = FormatMetric("Firm_Label_Price", null);
            ProfitText = FormatMetric("Firm_Label_Profit", null);
        }
        else
        {
            Series = BuildSeries(result);
            QuantityText = FormatMetric("Firm_Label_Quantity", result.QuantityPoint?.X);
            PriceText = FormatMetric("Firm_Label_Price", result.QuantityPoint?.Y);
            ProfitText = FormatMetric("Firm_Label_Profit", result.Profit);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(FirmResult result)
    {
        var list = new List<ISeries>
        {
            ChartSeriesBuilder.Line(Localization["Firm_Series_MarginalCost"], result.MarginalCost, SKColors.SteelBlue),
            ChartSeriesBuilder.Line(Localization["Firm_Series_AverageCost"], result.AverageCost, SKColors.DarkOrange),
            ChartSeriesBuilder.Line(Localization["Firm_Series_AverageVariableCost"], result.AverageVariableCost, SKColors.MediumPurple)
        };

        var priceLine = ChartSeriesBuilder.HorizontalLine(Localization["Firm_Series_PriceLine"], 0, FirmMaxQuantity, result.PriceLine, SKColors.Firebrick, SelectedMode.Value == FirmMode.LongRun);
        list.Add(priceLine);

        if (ShowTotalRevenue)
        {
            var totalRevenue = BuildTotalRevenue(result.PriceLine);
            list.Add(ChartSeriesBuilder.Line(Localization["Firm_Series_TotalRevenue"], totalRevenue, SKColors.SeaGreen));
        }

        if (result.QuantityPoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter(Localization["Firm_Series_OptimalQuantity"], result.QuantityPoint.Value, SKColors.Black));
        }

        return list;
    }

    private static IReadOnlyList<ChartPoint> BuildTotalRevenue(double price)
    {
        var points = new List<ChartPoint>();
        for (var q = 0d; q <= FirmMaxQuantity; q += 1d)
        {
            points.Add(new ChartPoint(q, price * q));
        }

        return points;
    }

    private SelectionOption<FirmMode>[] BuildModeOptions()
    {
        return
        [
            new SelectionOption<FirmMode>(Localization["Firm_Mode_ShortRun"], FirmMode.ShortRun),
            new SelectionOption<FirmMode>(Localization["Firm_Mode_LongRun"], FirmMode.LongRun)
        ];
    }

    private void UpdateAxisLabels()
    {
        if (XAxes.Length > 0)
        {
            XAxes[0].Name = Localization["Axis_QuantityLower"];
        }

        if (YAxes.Length > 0)
        {
            YAxes[0].Name = Localization["Firm_Axis_CostsPrice"];
        }
    }

    private void OnLocalizationChanged()
    {
        _modeOptions = BuildModeOptions();
        OnPropertyChanged(nameof(ModeOptions));
        if (SelectedMode != null)
        {
            SelectedMode = _modeOptions.First(o => o.Value == SelectedMode.Value);
        }

        UpdateAxisLabels();
        UpdateCurrentPriceText();
        Recalculate();
    }

    private void UpdateCurrentPriceText()
    {
        CurrentPriceText = string.Format(Localization.CurrentCulture, Localization["Format_CurrentValue"], Price);
    }
}
