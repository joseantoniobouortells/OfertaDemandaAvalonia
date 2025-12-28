using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using OfertaDemanda.Core.Models;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class MarketViewModel : ViewModelBase
{
    private bool _suppressUpdates;

    [ObservableProperty]
    private string demandExpression = "100 - 0.5q";

    [ObservableProperty]
    private string supplyExpression = "20 + 0.5q";

    [ObservableProperty]
    private double demandShock = 0;

    [ObservableProperty]
    private double supplyShock = 0;

    [ObservableProperty]
    private double tax = 0;

    [ObservableProperty]
    private string equilibriumText = "Q*: —";

    [ObservableProperty]
    private string priceConsumerText = "P_c: —";

    [ObservableProperty]
    private string priceProducerText = "P_p: —";

    [ObservableProperty]
    private string consumerSurplusText = "CS: —";

    [ObservableProperty]
    private string producerSurplusText = "PS: —";

    [ObservableProperty]
    private string taxRevenueText = "Recaudación: —";

    [ObservableProperty]
    private string deadweightLossText = "Pérdida irrecup.: —";

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "Q", MinLimit = 0, MaxLimit = 100 }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "P", MinLimit = 0, MaxLimit = 150 }
    };

    public MarketViewModel() => ApplyDefaults();

    public void ApplyDefaults()
    {
        _suppressUpdates = true;
        DemandExpression = AppDefaults.Market.DemandExpression;
        SupplyExpression = AppDefaults.Market.SupplyExpression;
        DemandShock = AppDefaults.Market.DemandShock;
        SupplyShock = AppDefaults.Market.SupplyShock;
        Tax = AppDefaults.Market.Tax;
        _suppressUpdates = false;
        Recalculate();
    }

    partial void OnDemandExpressionChanged(string value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSupplyExpressionChanged(string value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnDemandShockChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSupplyShockChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnTaxChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(DemandExpression, "Demanda inversa", localErrors, out var demand) ||
            !TryParseExpression(SupplyExpression, "Oferta inversa", localErrors, out var supply))
        {
            UpdateState(null, localErrors);
            return;
        }

        var result = MarketCalculator.Calculate(new MarketParameters(demand!, supply!, DemandShock, SupplyShock, Tax));
        if (result.Errors.Count > 0)
        {
            localErrors.AddRange(result.Errors);
        }

        UpdateState(result, localErrors);
    }

    private void UpdateState(MarketResult? result, List<string> localErrors)
    {
        if (result == null)
        {
            Series = Array.Empty<ISeries>();
            EquilibriumText = "Q*: —";
            PriceConsumerText = "P_c: —";
            PriceProducerText = "P_p: —";
            ConsumerSurplusText = "CS: —";
            ProducerSurplusText = "PS: —";
            TaxRevenueText = "Recaudación: —";
            DeadweightLossText = "Pérdida irrecup.: —";
        }
        else
        {
            Series = BuildSeries(result);
            EquilibriumText = FormatMetric("Q*", result.Equilibrium?.X);
            PriceConsumerText = FormatMetric("P_c", result.Equilibrium?.Y);
            PriceProducerText = FormatMetric("P_p", result.ProducerPrice);
            ConsumerSurplusText = FormatMetric("CS", result.ConsumerSurplus);
            ProducerSurplusText = FormatMetric("PS", result.ProducerSurplus);
            TaxRevenueText = FormatMetric("Recaudación", result.TaxRevenue);
            DeadweightLossText = FormatMetric("Pérdida irrecup.", result.DeadweightLoss);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(MarketResult result)
    {
        var seriesList = new List<ISeries>();

        if (result.ConsumerArea.Count > 0 && result.Equilibrium.HasValue)
        {
            var csValues = result.ConsumerArea
                .Select(p => new ObservablePoint(p.X, p.BaseValue + p.OffsetValue))
                .ToArray();
            seriesList.Add(CreateFillSeries("CS", csValues, result.Equilibrium.Value.Y, SKColors.LightSkyBlue.WithAlpha(90)));
        }

        if (result.ProducerArea.Count > 0 && result.ProducerPrice.HasValue)
        {
            var psValues = result.ProducerArea
                .Select(p => new ObservablePoint(p.X, p.BaseValue))
                .ToArray();
            seriesList.Add(CreateFillSeries("PS", psValues, result.ProducerPrice.Value, SKColors.LightGreen.WithAlpha(90)));
        }

        seriesList.AddRange(BuildDeadweightSeries(result.DeadweightArea, SKColors.LightCoral.WithAlpha(110)));

        seriesList.Add(ChartSeriesBuilder.Line("Pd₀", result.DemandBase, SKColors.SlateGray, true));
        seriesList.Add(ChartSeriesBuilder.Line("Ps₀", result.SupplyBase, SKColors.DarkGray, true));
        seriesList.Add(ChartSeriesBuilder.Line("Pd₁", result.DemandShifted, SKColors.SteelBlue));
        seriesList.Add(ChartSeriesBuilder.Line("Ps₁", result.SupplyShifted, SKColors.OliveDrab));

        if (result.Equilibrium.HasValue)
        {
            seriesList.Add(ChartSeriesBuilder.Scatter("Q*", result.Equilibrium.Value, SKColors.Firebrick));
        }

        if (result.NoTaxEquilibrium.HasValue)
        {
            seriesList.Add(ChartSeriesBuilder.Scatter("Q sin impuesto", result.NoTaxEquilibrium.Value, SKColors.DarkGreen, 12));
        }

        return seriesList;
    }

    private IEnumerable<ISeries> BuildDeadweightSeries(IReadOnlyList<AreaSamplePoint> samples, SKColor color)
    {
        if (samples.Count == 0)
        {
            return Array.Empty<ISeries>();
        }

        var baseSeries = new StackedAreaSeries<ObservablePoint>
        {
            Values = samples.Select(p => new ObservablePoint(p.X, p.BaseValue)).ToArray(),
            Fill = null,
            Stroke = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Name = string.Empty,
            IsHoverable = false
        };

        var shadeSeries = new StackedAreaSeries<ObservablePoint>
        {
            Values = samples.Select(p => new ObservablePoint(p.X, p.OffsetValue)).ToArray(),
            Fill = new SolidColorPaint(color),
            Stroke = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Name = "DWL",
            IsHoverable = false
        };

        return new ISeries[] { baseSeries, shadeSeries };
    }

    private ISeries CreateFillSeries(string name, ObservablePoint[] values, double pivot, SKColor color)
    {
        return new LineSeries<ObservablePoint>
        {
            Name = name,
            Values = values,
            LineSmoothness = 0,
            Stroke = null,
            GeometryFill = null,
            GeometryStroke = null,
            Fill = new SolidColorPaint(color),
            Pivot = pivot,
            IsHoverable = false
        };
    }
}
