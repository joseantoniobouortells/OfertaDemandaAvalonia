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
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Math;
using OfertaDemanda.Shared.Settings;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class MarketViewModel : ViewModelBase
{
    private const double MarketMaxQuantity = 100d;
    private const string GroupMarketKey = "Market_ToggleGroup_Market";
    private const string GroupTaxKey = "Market_ToggleGroup_Tax";
    private const string GroupSurplusKey = "Market_ToggleGroup_Surplus";
    private const string GroupFirmKey = "Market_ToggleGroup_Firm";
    private const string GroupCostsKey = "Market_ToggleGroup_Costs";

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
    private bool _suppressPresetUpdates;
    private SelectionOption<MarketCostFunctionType>[] _costOptions = Array.Empty<SelectionOption<MarketCostFunctionType>>();
    private SelectionOption<MarketVisualizationPreset>[] _presetOptions = Array.Empty<SelectionOption<MarketVisualizationPreset>>();
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
    private string demandShockText = string.Empty;

    [ObservableProperty]
    private string supplyShockText = string.Empty;

    [ObservableProperty]
    private string taxText = string.Empty;

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
    private string totalCostFormulaLatex = string.Empty;

    [ObservableProperty]
    private string equilibriumText = string.Empty;

    [ObservableProperty]
    private string priceConsumerText = string.Empty;

    [ObservableProperty]
    private string priceProducerText = string.Empty;

    [ObservableProperty]
    private string consumerSurplusText = string.Empty;

    [ObservableProperty]
    private string producerSurplusText = string.Empty;

    [ObservableProperty]
    private string taxRevenueText = string.Empty;

    [ObservableProperty]
    private string deadweightLossText = string.Empty;

    [ObservableProperty]
    private string firmQuantityText = string.Empty;

    [ObservableProperty]
    private string firmMarginalCostText = string.Empty;

    [ObservableProperty]
    private string firmAverageVariableCostText = string.Empty;

    [ObservableProperty]
    private string firmAverageCostText = string.Empty;

    [ObservableProperty]
    private string firmProfitText = string.Empty;

    [ObservableProperty]
    private string firmStateText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    public ObservableCollection<ChartSeriesToggleGroup> ToggleGroups { get; } = new();

    public IRelayCommand ShowAllTogglesCommand { get; }

    public IRelayCommand HideAllTogglesCommand { get; }

    public IRelayCommand<ChartSeriesToggleGroup> ShowGroupCommand { get; }

    public IReadOnlyList<SelectionOption<MarketVisualizationPreset>> PresetOptions => _presetOptions;

    [ObservableProperty]
    private SelectionOption<MarketVisualizationPreset> selectedPreset = null!;

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

    public MarketViewModel(LocalizationService localization)
        : base(localization)
    {
        _costOptions = BuildCostOptions();
        _presetOptions = BuildPresetOptions();
        InitializeToggles();
        ShowAllTogglesCommand = new RelayCommand(() => SetAllToggles(true));
        HideAllTogglesCommand = new RelayCommand(() => SetAllToggles(false));
        ShowGroupCommand = new RelayCommand<ChartSeriesToggleGroup>(group => SetGroupToggles(group, true));
        Localization.CultureChanged += (_, _) => OnLocalizationChanged();
        UpdateAxisLabels();
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
        UpdateTotalCostFormula();
        SelectedPreset = _presetOptions.First(o => o.Value == MarketVisualizationPreset.Basic);
        ApplyPreset(SelectedPreset.Value);
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
        UpdateDemandShockText();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSupplyShockChanged(double value)
    {
        UpdateSupplyShockText();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnTaxChanged(double value)
    {
        UpdateTaxText();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSelectedCostTypeChanged(SelectionOption<MarketCostFunctionType> value)
    {
        OnPropertyChanged(nameof(IsCubicCostVisible));
        UpdateTotalCostFormula();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnFixedCostChanged(double value)
    {
        UpdateTotalCostFormula();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnLinearCostChanged(double value)
    {
        UpdateTotalCostFormula();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnQuadraticCostChanged(double value)
    {
        UpdateTotalCostFormula();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnCubicCostChanged(double value)
    {
        UpdateTotalCostFormula();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSelectedPresetChanged(SelectionOption<MarketVisualizationPreset> value)
    {
        if (_suppressPresetUpdates || _suppressUpdates)
        {
            return;
        }

        ApplyPreset(value.Value);
    }

    public IReadOnlyList<SelectionOption<MarketCostFunctionType>> CostTypeOptions => _costOptions;

    public bool IsCubicCostVisible => SelectedCostType != null && SelectedCostType.Value == MarketCostFunctionType.Cubic;

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(DemandExpression, Localization["Market_Parse_DemandInverse"], localErrors, out var demand) ||
            !TryParseExpression(SupplyExpression, Localization["Market_Parse_SupplyInverse"], localErrors, out var supply))
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
            EquilibriumText = FormatMetric("Market_Label_EquilibriumQuantity", null);
            PriceConsumerText = FormatMetric("Market_Label_PriceConsumer", null);
            PriceProducerText = FormatMetric("Market_Label_PriceProducer", null);
            ConsumerSurplusText = FormatMetric("Market_Label_ConsumerSurplus", null);
            ProducerSurplusText = FormatMetric("Market_Label_ProducerSurplus", null);
            TaxRevenueText = FormatMetric("Market_Label_TaxRevenue", null);
            DeadweightLossText = FormatMetric("Market_Label_DeadweightLoss", null);
            FirmQuantityText = FormatMetric("Market_Label_FirmQuantity", null);
            FirmMarginalCostText = FormatMetric("Market_Label_FirmMarginalCost", null);
            FirmAverageVariableCostText = FormatMetric("Market_Label_FirmAverageVariableCost", null);
            FirmAverageCostText = FormatMetric("Market_Label_FirmAverageCost", null);
            FirmProfitText = FormatMetric("Market_Label_FirmProfit", null);
            FirmStateText = BuildFirmStateText(null);
        }
        else
        {
            _lastMarketResult = result;
            _lastFirmResult = firmResult;
            Series = BuildSeries(result, firmResult);
            EquilibriumText = FormatMetric("Market_Label_EquilibriumQuantity", result.Equilibrium?.X);
            PriceConsumerText = FormatMetric("Market_Label_PriceConsumer", result.Equilibrium?.Y);
            PriceProducerText = FormatMetric("Market_Label_PriceProducer", result.ProducerPrice);
            ConsumerSurplusText = FormatMetric("Market_Label_ConsumerSurplus", result.ConsumerSurplus);
            ProducerSurplusText = FormatMetric("Market_Label_ProducerSurplus", result.ProducerSurplus);
            TaxRevenueText = FormatMetric("Market_Label_TaxRevenue", result.TaxRevenue);
            DeadweightLossText = FormatMetric("Market_Label_DeadweightLoss", result.DeadweightLoss);

            if (firmResult == null)
            {
                FirmQuantityText = FormatMetric("Market_Label_FirmQuantity", null);
                FirmMarginalCostText = FormatMetric("Market_Label_FirmMarginalCost", null);
                FirmAverageVariableCostText = FormatMetric("Market_Label_FirmAverageVariableCost", null);
                FirmAverageCostText = FormatMetric("Market_Label_FirmAverageCost", null);
                FirmProfitText = FormatMetric("Market_Label_FirmProfit", null);
                FirmStateText = BuildFirmStateText(null);
            }
            else
            {
                FirmQuantityText = FormatMetric("Market_Label_FirmQuantity", firmResult.OptimalQuantity);
                FirmMarginalCostText = FormatMetric("Market_Label_FirmMarginalCost", firmResult.MarginalCostAtOptimal);
                FirmAverageVariableCostText = FormatMetric("Market_Label_FirmAverageVariableCost", firmResult.AverageVariableCostAtOptimal);
                FirmAverageCostText = FormatMetric("Market_Label_FirmAverageCost", firmResult.AverageCostAtOptimal);
                FirmProfitText = FormatMetric("Market_Label_FirmProfit", firmResult.ProfitAtOptimal);
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
            AddSeries(seriesList, ToggleConsumerSurplus, CreateFillSeries(Localization["Market_Series_ConsumerSurplus"], csValues, result.Equilibrium.Value.Y, SKColors.LightSkyBlue.WithAlpha(90)));
        }

        if (result.ProducerArea.Count > 0 && result.ProducerPrice.HasValue)
        {
            var psValues = result.ProducerArea
                .Select(p => new ObservablePoint(p.X, p.BaseValue))
                .ToArray();
            AddSeries(seriesList, ToggleProducerSurplus, CreateFillSeries(Localization["Market_Series_ProducerSurplus"], psValues, result.ProducerPrice.Value, SKColors.LightGreen.WithAlpha(90)));
        }

        AddSeries(seriesList, ToggleDeadweight, BuildDeadweightSeries(result.DeadweightArea, SKColors.LightCoral.WithAlpha(110)));

        AddSeries(seriesList, ToggleDemandBase, ChartSeriesBuilder.Line(Localization["Market_Series_DemandBase"], result.DemandBase, SKColors.SlateGray, true));
        AddSeries(seriesList, ToggleSupplyBase, ChartSeriesBuilder.Line(Localization["Market_Series_SupplyBase"], result.SupplyBase, SKColors.DarkGray, true));
        AddSeries(seriesList, ToggleDemandShifted, ChartSeriesBuilder.Line(Localization["Market_Series_DemandShifted"], result.DemandShifted, SKColors.SteelBlue));
        AddSeries(seriesList, ToggleSupplyShifted, ChartSeriesBuilder.Line(Localization["Market_Series_SupplyShifted"], result.SupplyShifted, SKColors.OliveDrab));

        if (result.Equilibrium.HasValue)
        {
            AddSeries(seriesList, ToggleMarketEquilibrium, ChartSeriesBuilder.Scatter(Localization["Market_Series_Equilibrium"], result.Equilibrium.Value, SKColors.Firebrick));
            AddSeries(seriesList, BuildTaxSeries(result));
        }

        if (result.NoTaxEquilibrium.HasValue)
        {
            AddSeries(seriesList, ToggleNoTaxEquilibrium, ChartSeriesBuilder.Scatter(Localization["Market_Series_NoTaxEquilibrium"], result.NoTaxEquilibrium.Value, SKColors.DarkGreen, 12));
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

        AddSeries(seriesList, ToggleCostMarginal, ChartSeriesBuilder.Line(Localization["Market_Series_MarginalCost"], firmResult.MarginalCost, SKColors.SteelBlue));
        AddSeries(seriesList, ToggleCostAverageVariable, ChartSeriesBuilder.Line(Localization["Market_Series_AverageVariableCost"], firmResult.AverageVariableCost, SKColors.MediumPurple));
        AddSeries(seriesList, ToggleCostAverage, ChartSeriesBuilder.Line(Localization["Market_Series_AverageCost"], firmResult.AverageCost, SKColors.DarkOrange));
        AddSeries(seriesList, ToggleFirmPriceLine, ChartSeriesBuilder.HorizontalLine(Localization["Market_Series_PriceLine"], 0, MarketMaxQuantity, price, SKColors.Firebrick));

        if (firmResult.OptimalQuantity.HasValue && firmResult.OptimalQuantity.Value > 0)
        {
            AddSeries(seriesList, ToggleFirmOptimal, ChartSeriesBuilder.Scatter(Localization["Market_Series_FirmOptimalQuantity"], new ChartPoint(firmResult.OptimalQuantity.Value, price), SKColors.Black, 12));
        }

        if (firmResult.ShutdownQuantity.HasValue && firmResult.ShutdownPrice.HasValue)
        {
            AddSeries(seriesList, ToggleFirmShutdown, ChartSeriesBuilder.Scatter(Localization["Market_Series_ShutdownPrice"], new ChartPoint(firmResult.ShutdownQuantity.Value, firmResult.ShutdownPrice.Value), SKColors.DarkRed, 10));
        }

        if (firmResult.BreakEvenQuantities.Count > 0)
        {
            var index = 1;
            foreach (var q in firmResult.BreakEvenQuantities)
            {
                var label = firmResult.BreakEvenQuantities.Count == 1
                    ? Localization["Market_Series_BreakEven"]
                    : string.Format(Localization.CurrentCulture, Localization["Market_Series_BreakEvenIndexed"], index);
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
            Name = Localization["Market_Series_DeadweightShort"],
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
        var label = isProfit ? Localization["Market_Series_Profit"] : Localization["Market_Series_Loss"];
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

    private string BuildFirmStateText(MarketFirmResult? firmResult)
    {
        if (firmResult?.OptimalQuantity == null)
        {
            return FormatLabelValue(Localization["Market_Label_FirmState"], null);
        }

        if (firmResult.OptimalQuantity.Value <= 0)
        {
            return FormatLabelValue(Localization["Market_Label_FirmState"], Localization["Market_State_Closed"]);
        }

        if (!firmResult.ProfitAtOptimal.HasValue)
        {
            return FormatLabelValue(Localization["Market_Label_FirmState"], null);
        }

        var state = firmResult.ProfitAtOptimal.Value >= 0
            ? Localization["Market_State_Profit"]
            : Localization["Market_State_Loss"];
        return FormatLabelValue(Localization["Market_Label_FirmState"], state);
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
            (ToggleTaxPriceConsumer, ChartSeriesBuilder.HorizontalLine(Localization["Market_Series_PriceConsumer"], 0, q, priceConsumer, SKColors.IndianRed, true)),
            (ToggleTaxPriceProducer, ChartSeriesBuilder.HorizontalLine(Localization["Market_Series_PriceProducer"], 0, q, priceProducer, SKColors.SeaGreen, true)),
            (ToggleTaxWedge, ChartSeriesBuilder.VerticalLine(Localization["Market_Series_TaxWedge"], priceProducer, priceConsumer, q, SKColors.DimGray, true)),
            (ToggleTaxQuantity, ChartSeriesBuilder.Scatter(Localization["Market_Series_TaxQuantity"], new ChartPoint(q, 0), SKColors.SaddleBrown, 8))
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
            GroupMarketKey,
            new[]
            {
                CreateToggle(ToggleDemandBase, "Market_Toggle_DemandBase", GroupMarketKey, true),
                CreateToggle(ToggleDemandShifted, "Market_Toggle_DemandShifted", GroupMarketKey, true),
                CreateToggle(ToggleSupplyBase, "Market_Toggle_SupplyBase", GroupMarketKey, true),
                CreateToggle(ToggleSupplyShifted, "Market_Toggle_SupplyShifted", GroupMarketKey, true),
                CreateToggle(ToggleMarketEquilibrium, "Market_Toggle_Equilibrium", GroupMarketKey, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupTaxKey,
            new[]
            {
                CreateToggle(ToggleTaxPriceConsumer, "Market_Toggle_PriceConsumer", GroupTaxKey, true),
                CreateToggle(ToggleTaxPriceProducer, "Market_Toggle_PriceProducer", GroupTaxKey, true),
                CreateToggle(ToggleTaxQuantity, "Market_Toggle_TaxQuantity", GroupTaxKey, true),
                CreateToggle(ToggleTaxWedge, "Market_Toggle_TaxWedge", GroupTaxKey, true),
                CreateToggle(ToggleNoTaxEquilibrium, "Market_Toggle_NoTaxEquilibrium", GroupTaxKey, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupSurplusKey,
            new[]
            {
                CreateToggle(ToggleConsumerSurplus, "Market_Toggle_ConsumerSurplus", GroupSurplusKey, true),
                CreateToggle(ToggleProducerSurplus, "Market_Toggle_ProducerSurplus", GroupSurplusKey, true),
                CreateToggle(ToggleDeadweight, "Market_Toggle_DeadweightLoss", GroupSurplusKey, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupFirmKey,
            new[]
            {
                CreateToggle(ToggleFirmPriceLine, "Market_Toggle_PriceLine", GroupFirmKey, true),
                CreateToggle(ToggleFirmOptimal, "Market_Toggle_FirmOptimal", GroupFirmKey, true),
                CreateToggle(ToggleFirmShutdown, "Market_Toggle_FirmShutdown", GroupFirmKey, true),
                CreateToggle(ToggleFirmBreakEven, "Market_Toggle_FirmBreakEven", GroupFirmKey, true),
                CreateToggle(ToggleFirmProfitLoss, "Market_Toggle_FirmProfitLoss", GroupFirmKey, true)
            }));

        AddToggleGroup(new ChartSeriesToggleGroup(
            GroupCostsKey,
            new[]
            {
                CreateToggle(ToggleCostMarginal, "Market_Toggle_CostMarginal", GroupCostsKey, true),
                CreateToggle(ToggleCostAverageVariable, "Market_Toggle_CostAverageVariable", GroupCostsKey, true),
                CreateToggle(ToggleCostAverage, "Market_Toggle_CostAverage", GroupCostsKey, true)
            }));

        UpdateToggleLabels();
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
                        MarkPresetCustom();
                        RefreshSeries();
                    }
                }
            };
        }
    }

    private ChartSeriesToggle CreateToggle(string key, string labelKey, string groupKey, bool isVisible)
    {
        return new ChartSeriesToggle(key, labelKey, groupKey, isVisible);
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

    private void OnLocalizationChanged()
    {
        _costOptions = BuildCostOptions();
        OnPropertyChanged(nameof(CostTypeOptions));
        _presetOptions = BuildPresetOptions();
        OnPropertyChanged(nameof(PresetOptions));
        if (SelectedPreset != null)
        {
            var presetValue = SelectedPreset.Value;
            _suppressPresetUpdates = true;
            SelectedPreset = _presetOptions.First(o => o.Value == presetValue);
            _suppressPresetUpdates = false;
        }

        if (SelectedCostType != null)
        {
            SelectedCostType = _costOptions.First(o => o.Value == SelectedCostType.Value);
        }

        UpdateToggleLabels();
        UpdateAxisLabels();
        UpdateDemandShockText();
        UpdateSupplyShockText();
        UpdateTaxText();
        UpdateTotalCostFormula();
        Recalculate();
    }

    private SelectionOption<MarketCostFunctionType>[] BuildCostOptions()
    {
        return
        [
            new SelectionOption<MarketCostFunctionType>(Localization["Market_CostType_Quadratic"], MarketCostFunctionType.Quadratic),
            new SelectionOption<MarketCostFunctionType>(Localization["Market_CostType_Cubic"], MarketCostFunctionType.Cubic)
        ];
    }

    private void UpdateToggleLabels()
    {
        foreach (var group in ToggleGroups)
        {
            group.Label = Localization[group.LabelKey];
            foreach (var toggle in group.Items)
            {
                toggle.Label = Localization[toggle.LabelKey];
            }
        }
    }

    private SelectionOption<MarketVisualizationPreset>[] BuildPresetOptions()
    {
        return
        [
            new SelectionOption<MarketVisualizationPreset>(Localization["Market_Preset_Basic"], MarketVisualizationPreset.Basic),
            new SelectionOption<MarketVisualizationPreset>(Localization["Market_Preset_Tax"], MarketVisualizationPreset.Tax),
            new SelectionOption<MarketVisualizationPreset>(Localization["Market_Preset_Welfare"], MarketVisualizationPreset.Welfare),
            new SelectionOption<MarketVisualizationPreset>(Localization["Market_Preset_Complete"], MarketVisualizationPreset.Complete),
            new SelectionOption<MarketVisualizationPreset>(Localization["Market_Preset_Custom"], MarketVisualizationPreset.Custom)
        ];
    }

    private void ApplyPreset(MarketVisualizationPreset preset)
    {
        _suppressToggleUpdates = true;
        _suppressPresetUpdates = true;

        if (preset != MarketVisualizationPreset.Custom)
        {
            foreach (var toggle in _toggleLookup.Values)
            {
                toggle.IsVisible = false;
            }

            foreach (var key in GetPresetKeys(preset))
            {
                if (_toggleLookup.TryGetValue(key, out var toggle))
                {
                    toggle.IsVisible = true;
                }
            }
        }

        _suppressToggleUpdates = false;
        _suppressPresetUpdates = false;
        RefreshSeries();
    }

    private IReadOnlyList<string> GetPresetKeys(MarketVisualizationPreset preset)
    {
        return preset switch
        {
            MarketVisualizationPreset.Basic =>
            [
                ToggleDemandShifted,
                ToggleSupplyShifted,
                ToggleMarketEquilibrium
            ],
            MarketVisualizationPreset.Tax =>
            [
                ToggleDemandShifted,
                ToggleSupplyShifted,
                ToggleMarketEquilibrium,
                ToggleTaxPriceConsumer,
                ToggleTaxPriceProducer,
                ToggleTaxQuantity,
                ToggleTaxWedge,
                ToggleNoTaxEquilibrium
            ],
            MarketVisualizationPreset.Welfare =>
            [
                ToggleDemandShifted,
                ToggleSupplyShifted,
                ToggleMarketEquilibrium,
                ToggleConsumerSurplus,
                ToggleProducerSurplus,
                ToggleDeadweight
            ],
            MarketVisualizationPreset.Complete => _toggleLookup.Keys.ToArray(),
            MarketVisualizationPreset.Custom => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
    }

    private void MarkPresetCustom()
    {
        if (_suppressPresetUpdates)
        {
            return;
        }

        var custom = _presetOptions.First(o => o.Value == MarketVisualizationPreset.Custom);
        if (!Equals(SelectedPreset, custom))
        {
            _suppressPresetUpdates = true;
            SelectedPreset = custom;
            _suppressPresetUpdates = false;
        }
    }

    private void UpdateAxisLabels()
    {
        if (XAxes.Length > 0)
        {
            XAxes[0].Name = Localization["Axis_Quantity"];
        }

        if (YAxes.Length > 0)
        {
            YAxes[0].Name = Localization["Axis_Price"];
        }
    }

    private void UpdateDemandShockText()
    {
        DemandShockText = string.Format(Localization.CurrentCulture, Localization["Format_CurrentValue"], DemandShock);
    }

    private void UpdateSupplyShockText()
    {
        SupplyShockText = string.Format(Localization.CurrentCulture, Localization["Format_CurrentValue"], SupplyShock);
    }

    private void UpdateTaxText()
    {
        TaxText = string.Format(Localization.CurrentCulture, Localization["Format_CurrentValue"], Tax);
    }

    private void UpdateTotalCostFormula()
    {
        var costType = SelectedCostType?.Value ?? MarketCostFunctionType.Quadratic;
        TotalCostFormulaLatex = TotalCostFormulaBuilder.Build(
            costType,
            FixedCost,
            LinearCost,
            QuadraticCost,
            CubicCost,
            Localization.CurrentCulture);
    }
}
