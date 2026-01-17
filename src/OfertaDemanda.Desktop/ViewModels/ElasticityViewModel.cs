using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Core.Numerics;
using OfertaDemanda.Desktop.Services;
using OfertaDemanda.Shared.Settings;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class ElasticityViewModel : ViewModelBase
{
    private const double ElasticityCurveMinPrice = 10;
    private const double ElasticityCurveMaxPrice = 110;
    private const double ElasticityCurveStep = 2;
    private bool _suppressUpdates;
    private bool _updatingMode;
    private readonly MarketViewModel _market;

    [ObservableProperty]
    private double price = 50;

    [ObservableProperty]
    private string supplyExpression = AppDefaults.Elasticity.SupplyExpression;

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    [ObservableProperty]
    private IEnumerable<ISeries> elasticityCurveSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private bool isQuantityMode = true;

    [ObservableProperty]
    private bool isPriceMode;

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    [ObservableProperty]
    private string elasticityText = string.Empty;

    [ObservableProperty]
    private string marginalRevenueText = string.Empty;

    [ObservableProperty]
    private string marginalRevenueInterpretationText = string.Empty;

    [ObservableProperty]
    private string marketShockText = string.Empty;

    [ObservableProperty]
    private string priceText = string.Empty;

    public Axis[] ElasticityXAxes { get; } =
    {
        new Axis { Name = "P", MinLimit = ElasticityCurveMinPrice, MaxLimit = ElasticityCurveMaxPrice }
    };

    public Axis[] ElasticityYAxes { get; } =
    {
        new Axis { Name = "ε", MinLimit = 0 }
    };

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "q", MinLimit = 0, MaxLimit = 150 }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "P", MinLimit = 0, MaxLimit = 150 }
    };

    public MarketViewModel Market => _market;

    public ElasticityViewModel(MarketViewModel market, LocalizationService localization)
        : base(localization)
    {
        _market = market;
        _market.PropertyChanged += OnMarketPropertyChanged;
        Localization.CultureChanged += (_, _) => OnLocalizationChanged();
        UpdateAxisLabels();
        UpdateMarketShockText();
        ApplyDefaults();
    }

    public void ApplyDefaults()
    {
        _suppressUpdates = true;
        Price = AppDefaults.Elasticity.Price;
        SupplyExpression = AppDefaults.Elasticity.SupplyExpression;
        IsQuantityMode = true;
        IsPriceMode = false;
        _suppressUpdates = false;
        Recalculate();
    }

    partial void OnPriceChanged(double value)
    {
        UpdatePriceText();
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnSupplyExpressionChanged(string value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnIsQuantityModeChanged(bool value)
    {
        if (_updatingMode || !value)
        {
            return;
        }

        _updatingMode = true;
        IsPriceMode = false;
        _updatingMode = false;
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnIsPriceModeChanged(bool value)
    {
        if (_updatingMode || !value)
        {
            return;
        }

        _updatingMode = true;
        IsQuantityMode = false;
        _updatingMode = false;
        if (!_suppressUpdates) Recalculate();
    }

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(_market.DemandExpression, Localization["Elasticity_Parse_MarketDemand"], localErrors, out var demand))
        {
            UpdateState(
                null,
                Array.Empty<ChartPoint>(),
                Array.Empty<ChartPoint>(),
                Array.Empty<ChartPoint>(),
                Array.Empty<ChartPoint>(),
                localErrors);
            return;
        }

        ParsedExpression? supply = null;
        if (!TryParseExpression(SupplyExpression, Localization["Elasticity_Parse_SupplyInverse"], localErrors, out supply))
        {
            supply = null;
        }

        var result = ElasticityCalculator.Calculate(new ElasticityParameters(demand!, _market.DemandShock, Price));
        if (result.Errors.Count > 0)
        {
            localErrors.AddRange(result.Errors);
        }

        var supplyPoints = supply == null ? Array.Empty<ChartPoint>() : BuildSupplyPoints(supply);
        var elasticityCurve = BuildElasticityCurve(demand!, _market.DemandShock, localErrors);
        var marginalRevenueCurve = BuildMarginalRevenuePoints(demand!, _market.DemandShock);
        var marginalRevenuePriceCurve = BuildMarginalRevenueCurveByPrice(demand!, _market.DemandShock);
        UpdateState(result, supplyPoints, elasticityCurve, marginalRevenueCurve, marginalRevenuePriceCurve, localErrors);
    }

    private void UpdateState(
        ElasticityResult? result,
        IReadOnlyList<ChartPoint> supply,
        IReadOnlyList<ChartPoint> elasticityCurve,
        IReadOnlyList<ChartPoint> marginalRevenueCurve,
        IReadOnlyList<ChartPoint> marginalRevenuePriceCurve,
        List<string> localErrors)
    {
        if (result == null)
        {
            Series = Array.Empty<ISeries>();
            ElasticityCurveSeries = Array.Empty<ISeries>();
            ElasticityText = FormatMetric("Elasticity_Label_Value", null);
            MarginalRevenueText = FormatMetric("Elasticity_Label_MarginalRevenue", null);
            MarginalRevenueInterpretationText = FormatLabelValue("Elasticity_Label_Interpretation", null);
        }
        else
        {
            Series = BuildSeries(result, supply, marginalRevenueCurve, IsQuantityMode);
            var marginalRevenue = CalculateMarginalRevenue(result.Elasticity);
            ElasticityCurveSeries = BuildElasticitySeries(
                elasticityCurve,
                marginalRevenuePriceCurve,
                result.Elasticity,
                marginalRevenue,
                IsPriceMode);
            ElasticityText = FormatMetric("Elasticity_Label_Value", result.Elasticity);
            MarginalRevenueText = FormatMetric("Elasticity_Label_MarginalRevenue", marginalRevenue);
            MarginalRevenueInterpretationText = BuildInterpretationText(result.Elasticity, marginalRevenue);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(
        ElasticityResult result,
        IReadOnlyList<ChartPoint> supply,
        IReadOnlyList<ChartPoint> marginalRevenue,
        bool includeMarginalRevenue)
    {
        var list = new List<ISeries>
        {
            ChartSeriesBuilder.Line(Localization["Elasticity_Series_Demand"], result.Demand, SKColors.SteelBlue)
        };

        if (supply.Count > 0)
        {
            list.Add(ChartSeriesBuilder.Line(Localization["Elasticity_Series_Supply"], supply, SKColors.ForestGreen));
        }

        if (includeMarginalRevenue && marginalRevenue.Count > 0)
        {
            list.Add(ChartSeriesBuilder.Line(Localization["Elasticity_Series_MarginalRevenue"], marginalRevenue, SKColors.MediumPurple));
        }

        if (result.Point.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter(Localization["Elasticity_Series_Price"], result.Point.Value, SKColors.Firebrick));
        }

        return list;
    }

    private IEnumerable<ISeries> BuildElasticitySeries(
        IReadOnlyList<ChartPoint> elasticityCurve,
        IReadOnlyList<ChartPoint> marginalRevenuePriceCurve,
        double? elasticity,
        double? marginalRevenue,
        bool includeMarginalRevenue)
    {
        var list = new List<ISeries>();
        if (elasticityCurve.Count > 0)
        {
            list.Add(ChartSeriesBuilder.Line(Localization["Elasticity_Series_ElasticityCurve"], elasticityCurve, SKColors.DarkOrange));
        }

        if (includeMarginalRevenue && marginalRevenuePriceCurve.Count > 0)
        {
            list.Add(ChartSeriesBuilder.Line(
                Localization["Elasticity_Series_MarginalRevenueCurve"],
                marginalRevenuePriceCurve,
                SKColors.MediumPurple));
        }

        if (elasticity.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter(
                Localization["Elasticity_Series_ElasticityPoint"],
                new ChartPoint(Price, elasticity.Value),
                SKColors.Firebrick,
                12));
        }

        if (includeMarginalRevenue && marginalRevenue.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter(
                Localization["Elasticity_Series_MarginalRevenuePoint"],
                new ChartPoint(Price, marginalRevenue.Value),
                SKColors.MediumPurple,
                12));
        }

        return list;
    }

    private void OnMarketPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarketViewModel.DemandExpression) or nameof(MarketViewModel.DemandShock))
        {
            UpdateMarketShockText();
            Recalculate();
        }
    }

    private void UpdateMarketShockText()
    {
        MarketShockText = string.Format(Localization.CurrentCulture, Localization["Elasticity_Format_CurrentShock"], _market.DemandShock);
    }

    private void UpdatePriceText()
    {
        PriceText = string.Format(Localization.CurrentCulture, Localization["Elasticity_Format_CurrentPrice"], Price);
    }

    private double? CalculateMarginalRevenue(double? elasticity)
    {
        if (!elasticity.HasValue)
        {
            return null;
        }

        var value = elasticity.Value;
        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) < 1e-6)
        {
            return null;
        }

        return CalculateMarginalRevenue(Price, value);
    }

    private string BuildInterpretationText(double? elasticity, double? marginalRevenue)
    {
        if (!elasticity.HasValue || !marginalRevenue.HasValue)
        {
            return FormatLabelValue("Elasticity_Label_Interpretation", null);
        }

        var value = elasticity.Value;
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return FormatLabelValue("Elasticity_Label_Interpretation", null);
        }

        var key = value switch
        {
            > 1d => "Elasticity_Interpretation_Elastic",
            < 1d => "Elasticity_Interpretation_Inelastic",
            _ => "Elasticity_Interpretation_Unitary"
        };

        return FormatLabelValue("Elasticity_Label_Interpretation", Localization[key]);
    }

    private IReadOnlyList<ChartPoint> BuildSupplyPoints(ParsedExpression supply)
    {
        var points = new ChartPoint[150];
        for (var i = 0; i < points.Length; i++)
        {
            var q = i;
            points[i] = new ChartPoint(q, NumericMethods.Safe(supply.Evaluate(q)));
        }

        return points;
    }

    private IReadOnlyList<ChartPoint> BuildElasticityCurve(
        ParsedExpression demand,
        double demandShock,
        List<string> localErrors)
    {
        var elasticityPoints = new List<ChartPoint>();
        double Demand(double q) => NumericMethods.Safe(demand.Evaluate(q) + demandShock);

        for (var price = ElasticityCurveMinPrice; price <= ElasticityCurveMaxPrice; price += ElasticityCurveStep)
        {
            var elasticity = CalculateElasticityAtPrice(Demand, price);
            if (elasticity.HasValue)
            {
                elasticityPoints.Add(new ChartPoint(price, elasticity.Value));
            }
        }

        if (elasticityPoints.Count == 0)
        {
            localErrors.Add(Localization["Elasticity_Error_NoElasticityCurve"]);
        }

        return elasticityPoints;
    }

    private double? CalculateElasticityAtPrice(Func<double, double> demand, double price)
    {
        var qAtPrice = NumericMethods.FindRoot(q => demand(q) - price, 0, 500);
        if (double.IsNaN(qAtPrice) || qAtPrice <= 0)
        {
            return null;
        }

        var dpdq = NumericMethods.Derivative(demand, qAtPrice);
        if (double.IsNaN(dpdq) || Math.Abs(dpdq) < 1e-6)
        {
            return null;
        }

        var elasticity = NumericMethods.Safe(Math.Abs((1 / dpdq) * (price / qAtPrice)));
        if (double.IsNaN(elasticity) || double.IsInfinity(elasticity))
        {
            return null;
        }

        return elasticity;
    }

    private IReadOnlyList<ChartPoint> BuildMarginalRevenuePoints(ParsedExpression demand, double demandShock)
    {
        var points = new List<ChartPoint>();
        double Demand(double q) => NumericMethods.Safe(demand.Evaluate(q) + demandShock);

        for (var i = 1; i < 150; i++)
        {
            var q = i;
            var price = Demand(q);
            var dpdq = NumericMethods.Derivative(Demand, q);
            if (double.IsNaN(dpdq) || Math.Abs(dpdq) < 1e-6)
            {
                continue;
            }

            var elasticity = NumericMethods.Safe(Math.Abs((1 / dpdq) * (price / q)));
            if (double.IsNaN(elasticity) || double.IsInfinity(elasticity))
            {
                continue;
            }

            var marginalRevenue = CalculateMarginalRevenue(price, elasticity);
            if (marginalRevenue.HasValue)
            {
                points.Add(new ChartPoint(q, marginalRevenue.Value));
            }
        }

        return points;
    }

    private IReadOnlyList<ChartPoint> BuildMarginalRevenueCurveByPrice(ParsedExpression demand, double demandShock)
    {
        var points = new List<ChartPoint>();
        double Demand(double q) => NumericMethods.Safe(demand.Evaluate(q) + demandShock);

        for (var price = ElasticityCurveMinPrice; price <= ElasticityCurveMaxPrice; price += ElasticityCurveStep)
        {
            var elasticity = CalculateElasticityAtPrice(Demand, price);
            if (!elasticity.HasValue)
            {
                continue;
            }

            var marginalRevenue = CalculateMarginalRevenue(price, elasticity.Value);
            if (marginalRevenue.HasValue)
            {
                points.Add(new ChartPoint(price, marginalRevenue.Value));
            }
        }

        return points;
    }

    private double? CalculateMarginalRevenue(double price, double elasticity)
    {
        if (double.IsNaN(elasticity) || double.IsInfinity(elasticity) || Math.Abs(elasticity) < 1e-6)
        {
            return null;
        }

        var result = price * (1 - 1 / elasticity);
        return double.IsNaN(result) || double.IsInfinity(result) ? null : result;
    }

    private void UpdateAxisLabels()
    {
        if (XAxes.Length > 0)
        {
            XAxes[0].Name = Localization["Axis_QuantityLower"];
        }

        if (YAxes.Length > 0)
        {
            YAxes[0].Name = Localization["Axis_Price"];
        }

        if (ElasticityXAxes.Length > 0)
        {
            ElasticityXAxes[0].Name = Localization["Axis_Price"];
        }

        if (ElasticityYAxes.Length > 0)
        {
            ElasticityYAxes[0].Name = Localization["Elasticity_Axis_Elasticity"];
        }

    }

    private void OnLocalizationChanged()
    {
        UpdateAxisLabels();
        UpdateMarketShockText();
        UpdatePriceText();
        Recalculate();
    }
}
