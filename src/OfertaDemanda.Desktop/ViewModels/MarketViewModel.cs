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
    private const double MarketMaxQuantity = 100d;
    private bool _suppressUpdates;
    private readonly SelectionOption<MarketCostFunctionType>[] _costOptions =
    {
        new("Cuadrático", MarketCostFunctionType.Quadratic),
        new("Cúbico", MarketCostFunctionType.Cubic)
    };

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
    private SelectionOption<MarketCostFunctionType> selectedCostType = null!;

    [ObservableProperty]
    private double fixedCost;

    [ObservableProperty]
    private double linearCost;

    [ObservableProperty]
    private double quadraticCost;

    [ObservableProperty]
    private double cubicCost;

    [ObservableProperty]
    private bool showMarginalCost = true;

    [ObservableProperty]
    private bool showAverageCost = true;

    [ObservableProperty]
    private bool showAverageVariableCost = true;

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
    private string firmQuantityText = "q_f*: —";

    [ObservableProperty]
    private string firmMarginalCostText = "CMg(q_f*): —";

    [ObservableProperty]
    private string firmAverageVariableCostText = "CMeV(q_f*): —";

    [ObservableProperty]
    private string firmAverageCostText = "CMe(q_f*): —";

    [ObservableProperty]
    private string firmProfitText = "π(q_f*): —";

    [ObservableProperty]
    private string firmStateText = "Estado: —";

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "Q", MinLimit = 0, MaxLimit = MarketMaxQuantity }
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
        SelectedCostType = _costOptions.First(o => o.Value == AppDefaults.Market.CostType);
        FixedCost = AppDefaults.Market.FixedCost;
        LinearCost = AppDefaults.Market.LinearCost;
        QuadraticCost = AppDefaults.Market.QuadraticCost;
        CubicCost = AppDefaults.Market.CubicCost;
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

    partial void OnSelectedCostTypeChanged(SelectionOption<MarketCostFunctionType> value)
    {
        OnPropertyChanged(nameof(IsCubicCostVisible));
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnFixedCostChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnLinearCostChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnQuadraticCostChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnCubicCostChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowMarginalCostChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowAverageCostChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowAverageVariableCostChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    public IReadOnlyList<SelectionOption<MarketCostFunctionType>> CostTypeOptions => _costOptions;

    public bool IsCubicCostVisible => SelectedCostType != null && SelectedCostType.Value == MarketCostFunctionType.Cubic;

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(DemandExpression, "Demanda inversa", localErrors, out var demand) ||
            !TryParseExpression(SupplyExpression, "Oferta inversa", localErrors, out var supply))
        {
            UpdateState(null, null, localErrors);
            return;
        }

        var result = MarketCalculator.Calculate(new MarketParameters(demand!, supply!, DemandShock, SupplyShock, Tax));
        if (result.Errors.Count > 0)
        {
            localErrors.AddRange(result.Errors);
        }

        MarketFirmResult? firmResult = null;
        if (result.Equilibrium.HasValue)
        {
            var costParameters = new MarketCostParameters(
                SelectedCostType.Value,
                FixedCost,
                LinearCost,
                QuadraticCost,
                CubicCost);
            firmResult = MarketFirmCalculator.Calculate(costParameters, result.Equilibrium.Value.Y, MarketMaxQuantity);
            if (firmResult.Errors.Count > 0)
            {
                localErrors.AddRange(firmResult.Errors);
            }
        }

        UpdateState(result, firmResult, localErrors);
    }

    private void UpdateState(MarketResult? result, MarketFirmResult? firmResult, List<string> localErrors)
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
            FirmQuantityText = "q_f*: —";
            FirmMarginalCostText = "CMg(q_f*): —";
            FirmAverageVariableCostText = "CMeV(q_f*): —";
            FirmAverageCostText = "CMe(q_f*): —";
            FirmProfitText = "π(q_f*): —";
            FirmStateText = "Estado: —";
        }
        else
        {
            Series = BuildSeries(result, firmResult);
            EquilibriumText = FormatMetric("Q*", result.Equilibrium?.X);
            PriceConsumerText = FormatMetric("P_c", result.Equilibrium?.Y);
            PriceProducerText = FormatMetric("P_p", result.ProducerPrice);
            ConsumerSurplusText = FormatMetric("CS", result.ConsumerSurplus);
            ProducerSurplusText = FormatMetric("PS", result.ProducerSurplus);
            TaxRevenueText = FormatMetric("Recaudación", result.TaxRevenue);
            DeadweightLossText = FormatMetric("Pérdida irrecup.", result.DeadweightLoss);

            if (firmResult == null)
            {
                FirmQuantityText = "q_f*: —";
                FirmMarginalCostText = "CMg(q_f*): —";
                FirmAverageVariableCostText = "CMeV(q_f*): —";
                FirmAverageCostText = "CMe(q_f*): —";
                FirmProfitText = "π(q_f*): —";
                FirmStateText = "Estado: —";
            }
            else
            {
                FirmQuantityText = FormatMetric("q_f*", firmResult.OptimalQuantity);
                FirmMarginalCostText = FormatMetric("CMg(q_f*)", firmResult.MarginalCostAtOptimal);
                FirmAverageVariableCostText = FormatMetric("CMeV(q_f*)", firmResult.AverageVariableCostAtOptimal);
                FirmAverageCostText = FormatMetric("CMe(q_f*)", firmResult.AverageCostAtOptimal);
                FirmProfitText = FormatMetric("π(q_f*)", firmResult.ProfitAtOptimal);
                FirmStateText = BuildFirmStateText(firmResult);
            }
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(MarketResult result, MarketFirmResult? firmResult)
    {
        var seriesList = new List<ISeries>();
        if (result.Equilibrium.HasValue && firmResult != null)
        {
            seriesList.AddRange(BuildFirmOverlaySeries(result.Equilibrium.Value.Y, firmResult));
        }

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

    private IEnumerable<ISeries> BuildFirmOverlaySeries(double price, MarketFirmResult firmResult)
    {
        var seriesList = new List<ISeries>();

        if (firmResult.OptimalQuantity.HasValue &&
            firmResult.OptimalQuantity.Value > 0 &&
            firmResult.AverageCostAtOptimal.HasValue)
        {
            var isProfit = price >= firmResult.AverageCostAtOptimal.Value;
            seriesList.AddRange(BuildProfitLossSeries(
                firmResult.OptimalQuantity.Value,
                price,
                firmResult.AverageCostAtOptimal.Value,
                isProfit));
        }

        if (ShowMarginalCost)
        {
            seriesList.Add(ChartSeriesBuilder.Line("CMg", firmResult.MarginalCost, SKColors.SteelBlue));
        }

        if (ShowAverageVariableCost)
        {
            seriesList.Add(ChartSeriesBuilder.Line("CMeV", firmResult.AverageVariableCost, SKColors.MediumPurple));
        }

        if (ShowAverageCost)
        {
            seriesList.Add(ChartSeriesBuilder.Line("CMe", firmResult.AverageCost, SKColors.DarkOrange));
        }

        seriesList.Add(ChartSeriesBuilder.HorizontalLine("P*", 0, MarketMaxQuantity, price, SKColors.Firebrick));

        if (firmResult.OptimalQuantity.HasValue && firmResult.OptimalQuantity.Value > 0)
        {
            seriesList.Add(ChartSeriesBuilder.Scatter("q_f*", new ChartPoint(firmResult.OptimalQuantity.Value, price), SKColors.Black, 12));
        }

        if (firmResult.ShutdownQuantity.HasValue && firmResult.ShutdownPrice.HasValue)
        {
            seriesList.Add(ChartSeriesBuilder.Scatter("P cierre", new ChartPoint(firmResult.ShutdownQuantity.Value, firmResult.ShutdownPrice.Value), SKColors.DarkRed, 10));
        }

        if (firmResult.BreakEvenQuantities.Count > 0)
        {
            var index = 1;
            foreach (var q in firmResult.BreakEvenQuantities)
            {
                var label = firmResult.BreakEvenQuantities.Count == 1 ? "P = CMe" : $"P = CMe {index}";
                seriesList.Add(ChartSeriesBuilder.Scatter(label, new ChartPoint(q, price), SKColors.DarkSlateGray, 10));
                index++;
            }
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
            Name = "PEI",
            IsHoverable = false
        };

        return new ISeries[] { baseSeries, shadeSeries };
    }

    private IEnumerable<ISeries> BuildProfitLossSeries(double quantity, double price, double averageCost, bool isProfit)
    {
        if (quantity <= 0)
        {
            return Array.Empty<ISeries>();
        }

        var bottom = Math.Min(price, averageCost);
        var top = Math.Max(price, averageCost);
        var samples = new[]
        {
            new AreaSamplePoint(0, bottom, top - bottom),
            new AreaSamplePoint(quantity, bottom, top - bottom)
        };

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

        var fillColor = isProfit ? SKColors.LightGreen.WithAlpha(90) : SKColors.LightCoral.WithAlpha(90);
        var label = isProfit ? "Beneficio" : "Pérdida";
        var shadeSeries = new StackedAreaSeries<ObservablePoint>
        {
            Values = samples.Select(p => new ObservablePoint(p.X, p.OffsetValue)).ToArray(),
            Fill = new SolidColorPaint(fillColor),
            Stroke = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Name = label,
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

    private string BuildFirmStateText(MarketFirmResult firmResult)
    {
        if (!firmResult.OptimalQuantity.HasValue)
        {
            return "Estado: —";
        }

        if (firmResult.OptimalQuantity.Value <= 0)
        {
            return "Estado: Cierre";
        }

        if (!firmResult.ProfitAtOptimal.HasValue)
        {
            return "Estado: —";
        }

        return firmResult.ProfitAtOptimal.Value >= 0
            ? "Estado: Beneficio"
            : "Estado: Pérdida";
    }
}
