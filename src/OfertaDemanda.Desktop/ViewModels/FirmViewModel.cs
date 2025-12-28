using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using OfertaDemanda.Core.Models;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class FirmViewModel : ViewModelBase
{
    private bool _suppressUpdates;
    private readonly SelectionOption<FirmMode>[] _modeOptions =
    {
        new("CP", FirmMode.ShortRun),
        new("LP", FirmMode.LongRun)
    };

    [ObservableProperty]
    private string costExpression = "200 + 10q + 0.5q^2";

    [ObservableProperty]
    private double price = 40;

    [ObservableProperty]
    private SelectionOption<FirmMode> selectedMode = null!;

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    [ObservableProperty]
    private string quantityText = "q*: —";

    [ObservableProperty]
    private string priceText = "P: —";

    [ObservableProperty]
    private string profitText = "Beneficio: —";

    public IReadOnlyList<SelectionOption<FirmMode>> ModeOptions => _modeOptions;

    public bool IsPriceEditable => SelectedMode.Value == FirmMode.ShortRun;

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "q", MinLimit = 0, MaxLimit = 60 }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "Costes / P", MinLimit = 0, MaxLimit = 120 }
    };

    public FirmViewModel() => ApplyDefaults();

    public void ApplyDefaults()
    {
        _suppressUpdates = true;
        CostExpression = AppDefaults.Firm.CostExpression;
        Price = AppDefaults.Firm.Price;
        SelectedMode = _modeOptions.First(o => o.Value == AppDefaults.Firm.Mode);
        _suppressUpdates = false;
        Recalculate();
    }

    partial void OnCostExpressionChanged(string value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnPriceChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSelectedModeChanged(SelectionOption<FirmMode> value)
    {
        OnPropertyChanged(nameof(IsPriceEditable));
        if (!_suppressUpdates) Recalculate();
    }

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(CostExpression, "CT(q)", localErrors, out var cost))
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
            QuantityText = "q*: —";
            PriceText = "P: —";
            ProfitText = "Beneficio: —";
        }
        else
        {
            Series = BuildSeries(result);
            QuantityText = FormatMetric("q*", result.QuantityPoint?.X);
            PriceText = FormatMetric("P", result.QuantityPoint?.Y);
            ProfitText = FormatMetric("Beneficio", result.Profit);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(FirmResult result)
    {
        var list = new List<ISeries>
        {
            ChartSeriesBuilder.Line("CMg", result.MarginalCost, SKColors.SteelBlue),
            ChartSeriesBuilder.Line("CMe", result.AverageCost, SKColors.DarkOrange),
            ChartSeriesBuilder.Line("CMeV", result.AverageVariableCost, SKColors.MediumPurple)
        };

        var priceLine = ChartSeriesBuilder.HorizontalLine("Precio", 0, 60, result.PriceLine, SKColors.Firebrick, SelectedMode.Value == FirmMode.LongRun);
        list.Add(priceLine);

        if (result.QuantityPoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter("q*", result.QuantityPoint.Value, SKColors.Black));
        }

        return list;
    }
}
