using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private const string GroupMarket = "Mercado";
    private const string GroupTax = "Impuesto";
    private const string GroupSurplus = "Excedentes";
    private const string GroupFirm = "Empresa competitiva";
    private const string GroupCosts = "Costes";

    private const string ToggleDemandBase = "demand_base";
    private const string ToggleDemandShifted = "demand_shifted";
    private const string ToggleSupplyBase = "supply_base";
    private const string ToggleSupplyShifted = "supply_shifted";
    private const string ToggleMarketEquilibrium = "market_equilibrium";
    private const string ToggleTaxPriceConsumer = "tax_price_consumer";
    private const string ToggleTaxPriceProducer = "tax_price_producer";
    private const string ToggleTaxQuantity = "tax_quantity";
    private const string ToggleTaxWedge = "tax_wedge";
    private const string ToggleNoTaxEquilibrium = "no_tax_equilibrium";
    private const string ToggleConsumerSurplus = "consumer_surplus";
    private const string ToggleProducerSurplus = "producer_surplus";
    private const string ToggleDeadweight = "deadweight";
    private const string ToggleFirmPriceLine = "firm_price_line";
    private const string ToggleFirmOptimal = "firm_optimal";
    private const string ToggleFirmShutdown = "firm_shutdown";
    private const string ToggleFirmBreakEven = "firm_break_even";
    private const string ToggleFirmProfitLoss = "firm_profit_loss";
    private const string ToggleCostMarginal = "cost_marginal";
    private const string ToggleCostAverageVariable = "cost_average_variable";
    private const string ToggleCostAverage = "cost_average";

    private bool _suppressUpdates;
    private bool _suppressToggleUpdates;
    private readonly SelectionOption<MarketCostFunctionType>[] _costOptions =
    {
        new("Cuadrático", MarketCostFunctionType.Quadratic),
        new("Cúbico", MarketCostFunctionType.Cubic)
    };
    private readonly Dictionary<string, ChartSeriesToggle> _toggleLookup = new();
    private MarketResult? _lastMarketResult;
    private MarketFirmResult? _lastFirmResult;

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

    public ObservableCollection<ChartSeriesToggleGroup> ToggleGroups { get; } = new();

    public IRelayCommand ShowAllTogglesCommand { get; }

    public IRelayCommand HideAllTogglesCommand { get; }

    public IRelayCommand<ChartSeriesToggleGroup> ShowGroupCommand { get; }

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

    public MarketViewModel()
    {
        InitializeToggles();
        ShowAllTogglesCommand = new RelayCommand(() => SetAllToggles(true));
        HideAllTogglesCommand = new RelayCommand(() => SetAllToggles(false));
        ShowGroupCommand = new RelayCommand<ChartSeriesToggleGroup>(group => SetGroupToggles(group, true));
        ApplyDefaults();
    }

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
        ResetToggleDefaults();
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
            _lastMarketResult = null;
            _lastFirmResult = null;
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
            _lastMarketResult = result;
            _lastFirmResult = firmResult;
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
        var seriesList = new List<(string Key, ISeries Series)>();
        if (result.Equilibrium.HasValue && firmResult != null)
        {
            AddSeries(seriesList, BuildFirmOverlaySeries(result.Equilibrium.Value.Y, firmResult));
        }

        if (result.ConsumerArea.Count > 0 && result.Equilibrium.HasValue)
        {
            var csValues = result.ConsumerArea
                .Select(p => new ObservablePoint(p.X, p.BaseValue + p.OffsetValue))
                .ToArray();
            AddSeries(seriesList, ToggleConsumerSurplus, CreateFillSeries("CS", csValues, result.Equilibrium.Value.Y, SKColors.LightSkyBlue.WithAlpha(90)));
        }

        if (result.ProducerArea.Count > 0 && result.ProducerPrice.HasValue)
        {
            var psValues = result.ProducerArea
                .Select(p => new ObservablePoint(p.X, p.BaseValue))
                .ToArray();
            AddSeries(seriesList, ToggleProducerSurplus, CreateFillSeries("PS", psValues, result.ProducerPrice.Value, SKColors.LightGreen.WithAlpha(90)));
        }

        AddSeries(seriesList, ToggleDeadweight, BuildDeadweightSeries(result.DeadweightArea, SKColors.LightCoral.WithAlpha(110)));

        AddSeries(seriesList, ToggleDemandBase, ChartSeriesBuilder.Line("Pd₀", result.DemandBase, SKColors.SlateGray, true));
        AddSeries(seriesList, ToggleSupplyBase, ChartSeriesBuilder.Line("Ps₀", result.SupplyBase, SKColors.DarkGray, true));
        AddSeries(seriesList, ToggleDemandShifted, ChartSeriesBuilder.Line("Pd₁", result.DemandShifted, SKColors.SteelBlue));
        AddSeries(seriesList, ToggleSupplyShifted, ChartSeriesBuilder.Line("Ps₁", result.SupplyShifted, SKColors.OliveDrab));

        if (result.Equilibrium.HasValue)
        {
            AddSeries(seriesList, ToggleMarketEquilibrium, ChartSeriesBuilder.Scatter("Q*", result.Equilibrium.Value, SKColors.Firebrick));
            AddSeries(seriesList, BuildTaxSeries(result));
        }

        if (result.NoTaxEquilibrium.HasValue)
        {
            AddSeries(seriesList, ToggleNoTaxEquilibrium, ChartSeriesBuilder.Scatter("Q sin impuesto", result.NoTaxEquilibrium.Value, SKColors.DarkGreen, 12));
        }

        return FilterSeries(seriesList);
    }

    private IEnumerable<(string Key, ISeries Series)> BuildFirmOverlaySeries(double price, MarketFirmResult firmResult)
    {
        var seriesList = new List<(string Key, ISeries Series)>();

        if (firmResult.OptimalQuantity.HasValue &&
            firmResult.OptimalQuantity.Value > 0 &&
            firmResult.AverageCostAtOptimal.HasValue)
        {
            var isProfit = price >= firmResult.AverageCostAtOptimal.Value;
            AddSeries(seriesList, ToggleFirmProfitLoss, BuildProfitLossSeries(
                firmResult.OptimalQuantity.Value,
                price,
                firmResult.AverageCostAtOptimal.Value,
                isProfit));
        }

        AddSeries(seriesList, ToggleCostMarginal, ChartSeriesBuilder.Line("CMg", firmResult.MarginalCost, SKColors.SteelBlue));
        AddSeries(seriesList, ToggleCostAverageVariable, ChartSeriesBuilder.Line("CMeV", firmResult.AverageVariableCost, SKColors.MediumPurple));
        AddSeries(seriesList, ToggleCostAverage, ChartSeriesBuilder.Line("CMe", firmResult.AverageCost, SKColors.DarkOrange));
        AddSeries(seriesList, ToggleFirmPriceLine, ChartSeriesBuilder.HorizontalLine("P*", 0, MarketMaxQuantity, price, SKColors.Firebrick));

        if (firmResult.OptimalQuantity.HasValue && firmResult.OptimalQuantity.Value > 0)
        {
            AddSeries(seriesList, ToggleFirmOptimal, ChartSeriesBuilder.Scatter("q_f*", new ChartPoint(firmResult.OptimalQuantity.Value, price), SKColors.Black, 12));
        }

        if (firmResult.ShutdownQuantity.HasValue && firmResult.ShutdownPrice.HasValue)
        {
            AddSeries(seriesList, ToggleFirmShutdown, ChartSeriesBuilder.Scatter("P cierre", new ChartPoint(firmResult.ShutdownQuantity.Value, firmResult.ShutdownPrice.Value), SKColors.DarkRed, 10));
        }

        if (firmResult.BreakEvenQuantities.Count > 0)
        {
            var index = 1;
            foreach (var q in firmResult.BreakEvenQuantities)
            {
                var label = firmResult.BreakEvenQuantities.Count == 1 ? "P = CMe" : $"P = CMe {index}";
                AddSeries(seriesList, ToggleFirmBreakEven, ChartSeriesBuilder.Scatter(label, new ChartPoint(q, price), SKColors.DarkSlateGray, 10));
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

    private IEnumerable<(string Key, ISeries Series)> BuildTaxSeries(MarketResult result)
    {
        if (!result.Equilibrium.HasValue || !result.ProducerPrice.HasValue)
        {
            return Array.Empty<(string Key, ISeries Series)>();
        }

        var eq = result.Equilibrium.Value;
        var priceConsumer = eq.Y;
        var priceProducer = result.ProducerPrice.Value;
        var q = eq.X;

        return new (string Key, ISeries Series)[]
        {
            (ToggleTaxPriceConsumer, ChartSeriesBuilder.HorizontalLine("P_c", 0, q, priceConsumer, SKColors.IndianRed, true)),
            (ToggleTaxPriceProducer, ChartSeriesBuilder.HorizontalLine("P_p", 0, q, priceProducer, SKColors.SeaGreen, true)),
            (ToggleTaxWedge, ChartSeriesBuilder.VerticalLine("Cuña", priceProducer, priceConsumer, q, SKColors.DimGray, true)),
            (ToggleTaxQuantity, ChartSeriesBuilder.Scatter("Q con impuesto", new ChartPoint(q, 0), SKColors.SaddleBrown, 8))
        };
    }

    private IEnumerable<ISeries> FilterSeries(IEnumerable<(string Key, ISeries Series)> seriesList)
    {
        var visibleKeys = _toggleLookup
            .Where(kvp => kvp.Value.IsVisible)
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.Ordinal);

        return seriesList
            .Where(entry => visibleKeys.Contains(entry.Key))
            .Select(entry => entry.Series)
            .ToArray();
    }

    private void AddSeries(List<(string Key, ISeries Series)> list, string key, ISeries series)
    {
        list.Add((key, series));
    }

    private void AddSeries(List<(string Key, ISeries Series)> list, IEnumerable<(string Key, ISeries Series)> series)
    {
        list.AddRange(series);
    }

    private void AddSeries(List<(string Key, ISeries Series)> list, string key, IEnumerable<ISeries> series)
    {
        foreach (var item in series)
        {
            list.Add((key, item));
        }
    }

    private void InitializeToggles()
    {
        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupMarket,
            new[]
            {
                CreateToggle(ToggleDemandBase, "Demanda Pd₀", GroupMarket, true),
                CreateToggle(ToggleDemandShifted, "Demanda Pd₁", GroupMarket, true),
                CreateToggle(ToggleSupplyBase, "Oferta Ps₀", GroupMarket, true),
                CreateToggle(ToggleSupplyShifted, "Oferta Ps₁", GroupMarket, true),
                CreateToggle(ToggleMarketEquilibrium, "Equilibrio (Q*, P*)", GroupMarket, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupTax,
            new[]
            {
                CreateToggle(ToggleTaxPriceConsumer, "Precio consumidor P_c", GroupTax, true),
                CreateToggle(ToggleTaxPriceProducer, "Precio productor P_p", GroupTax, true),
                CreateToggle(ToggleTaxQuantity, "Q con impuesto", GroupTax, true),
                CreateToggle(ToggleTaxWedge, "Cuña fiscal", GroupTax, true),
                CreateToggle(ToggleNoTaxEquilibrium, "Q sin impuesto", GroupTax, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupSurplus,
            new[]
            {
                CreateToggle(ToggleConsumerSurplus, "Excedente consumidor (CS)", GroupSurplus, true),
                CreateToggle(ToggleProducerSurplus, "Excedente productor (PS)", GroupSurplus, true),
                CreateToggle(ToggleDeadweight, "Pérdida irrecuperable", GroupSurplus, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupFirm,
            new[]
            {
                CreateToggle(ToggleFirmPriceLine, "Línea de precio P*", GroupFirm, true),
                CreateToggle(ToggleFirmOptimal, "q_f*", GroupFirm, true),
                CreateToggle(ToggleFirmShutdown, "Punto de cierre (min CMeV)", GroupFirm, true),
                CreateToggle(ToggleFirmBreakEven, "P = CMe", GroupFirm, true),
                CreateToggle(ToggleFirmProfitLoss, "Área beneficio/pérdida", GroupFirm, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupCosts,
            new[]
            {
                CreateToggle(ToggleCostMarginal, "CMg", GroupCosts, true),
                CreateToggle(ToggleCostAverageVariable, "CMeV", GroupCosts, true),
                CreateToggle(ToggleCostAverage, "CMe", GroupCosts, true)
            }));
    }

    private void AddToggleGroup(ChartSeriesToggleGroup group)
    {
        ToggleGroups.Add(group);
        foreach (var toggle in group.Items)
        {
            _toggleLookup[toggle.Key] = toggle;
            toggle.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ChartSeriesToggle.IsVisible))
                {
                    if (!_suppressToggleUpdates)
                    {
                        RefreshSeries();
                    }
                }
            };
        }
    }

    private ChartSeriesToggle CreateToggle(string key, string label, string group, bool isVisible)
    {
        return new ChartSeriesToggle(key, label, group, isVisible);
    }

    private void ResetToggleDefaults()
    {
        _suppressToggleUpdates = true;
        foreach (var toggle in _toggleLookup.Values)
        {
            toggle.IsVisible = true;
        }

        _suppressToggleUpdates = false;
        RefreshSeries();
    }

    private void SetAllToggles(bool isVisible)
    {
        _suppressToggleUpdates = true;
        foreach (var toggle in _toggleLookup.Values)
        {
            toggle.IsVisible = isVisible;
        }

        _suppressToggleUpdates = false;
        RefreshSeries();
    }

    private void SetGroupToggles(ChartSeriesToggleGroup? group, bool isVisible)
    {
        if (group == null)
        {
            return;
        }

        _suppressToggleUpdates = true;
        foreach (var toggle in group.Items)
        {
            toggle.IsVisible = isVisible;
        }

        _suppressToggleUpdates = false;
        RefreshSeries();
    }

    private void RefreshSeries()
    {
        if (_lastMarketResult == null)
        {
            Series = Array.Empty<ISeries>();
            return;
        }

        Series = BuildSeries(_lastMarketResult, _lastFirmResult);
    }
}
