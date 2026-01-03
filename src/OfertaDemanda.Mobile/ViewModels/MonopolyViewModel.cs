using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Core.Numerics;
using OfertaDemanda.Mobile.Services;
using OfertaDemanda.Shared.Settings;
using SkiaSharp;

namespace OfertaDemanda.Mobile.ViewModels;

public partial class MonopolyViewModel : ViewModelBase
{
    private const int IsoprofitSamples = 140;
    private const double IsoprofitEpsilon = 0.1;
    private const int AverageCostSamples = 120;

    private bool _suppressUpdates;

    [ObservableProperty]
    private string demandExpression = "120 - q";

    [ObservableProperty]
    private string costExpression = "100 + 10q + 0.2q^2";

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    [ObservableProperty]
    private string monopolyQuantityText = string.Empty;

    [ObservableProperty]
    private string monopolyPriceText = string.Empty;

    [ObservableProperty]
    private string profitText = string.Empty;

    [ObservableProperty]
    private string competitiveQuantityText = string.Empty;

    [ObservableProperty]
    private string deadweightLossText = string.Empty;

    [ObservableProperty]
    private string isoprofitExplanation = string.Empty;

    [ObservableProperty]
    private bool showIsoprofitCurve = true;

    [ObservableProperty]
    private bool showGuideLines = true;

    [ObservableProperty]
    private bool showDeadweightArea = true;

    [ObservableProperty]
    private bool showAverageCost = true;

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "q", MinLimit = 0, MaxLimit = 100 }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "P", MinLimit = 0, MaxLimit = 150 }
    };

    public MonopolyViewModel(LocalizationService localization)
        : base(localization)
    {
        Localization.CultureChanged += (_, _) => OnLocalizationChanged();
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += (_, _) => Recalculate();
        }
        UpdateAxisLabels();
        ApplyDefaults();
    }

    public void ApplyDefaults()
    {
        _suppressUpdates = true;
        DemandExpression = AppDefaults.Monopoly.DemandExpression;
        CostExpression = AppDefaults.Monopoly.CostExpression;
        ShowIsoprofitCurve = true;
        ShowGuideLines = true;
        ShowDeadweightArea = true;
        ShowAverageCost = true;
        _suppressUpdates = false;
        Recalculate();
    }

    partial void OnDemandExpressionChanged(string value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnCostExpressionChanged(string value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowIsoprofitCurveChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowGuideLinesChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowDeadweightAreaChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    partial void OnShowAverageCostChanged(bool value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(DemandExpression, Localization["Monopoly_Parse_Demand"], localErrors, out var demand) ||
            !TryParseExpression(CostExpression, Localization["Monopoly_Parse_TotalCost"], localErrors, out var cost))
        {
            UpdateState(null, null, localErrors);
            return;
        }

        var result = MonopolyCalculator.Calculate(new MonopolyParameters(demand!, cost!));
        if (result.Errors.Count > 0)
        {
            localErrors.AddRange(result.Errors);
        }

        UpdateState(result, cost, localErrors);
    }

    private void UpdateState(MonopolyResult? result, OfertaDemanda.Core.Expressions.ParsedExpression? cost, List<string> localErrors)
    {
        if (result == null)
        {
            Series = Array.Empty<ISeries>();
            MonopolyQuantityText = FormatMetric("Monopoly_Label_Quantity", null);
            MonopolyPriceText = FormatMetric("Monopoly_Label_Price", null);
            ProfitText = FormatMetric("Monopoly_Label_Profit", null);
            CompetitiveQuantityText = FormatMetric("Monopoly_Label_CompetitiveQuantity", null);
            DeadweightLossText = FormatMetric("Monopoly_Label_DeadweightLoss", null);
            IsoprofitExplanation = BuildIsoprofitExplanation(null);
        }
        else
        {
            Series = BuildSeries(result, cost);
            MonopolyQuantityText = FormatMetric("Monopoly_Label_Quantity", result.MonopolyPoint?.X);
            MonopolyPriceText = FormatMetric("Monopoly_Label_Price", result.MonopolyPoint?.Y);
            ProfitText = FormatMetric("Monopoly_Label_Profit", result.Profit);
            CompetitiveQuantityText = FormatMetric("Monopoly_Label_CompetitiveQuantity", result.CompetitivePoint?.X);
            DeadweightLossText = FormatMetric("Monopoly_Label_DeadweightLoss", result.DeadweightLoss);
            IsoprofitExplanation = BuildIsoprofitExplanation(result.Profit);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(MonopolyResult result, OfertaDemanda.Core.Expressions.ParsedExpression? cost)
    {
        var list = new List<ISeries>
        {
            ChartSeriesBuilder.Line(Localization["Monopoly_Series_Demand"], result.Demand, SKColors.SteelBlue),
            ChartSeriesBuilder.Line(Localization["Monopoly_Series_MarginalRevenue"], result.MarginalRevenue, SKColors.DarkOrange),
            ChartSeriesBuilder.Line(Localization["Monopoly_Series_MarginalCost"], result.MarginalCost, SKColors.OliveDrab)
        };

        if (ShowIsoprofitCurve && cost != null && result.MonopolyPoint.HasValue && result.Profit.HasValue)
        {
            var (qMin, qMax) = GetChartQRange();
            var isoprofit = BuildIsoprofitPoints(cost, result.Profit.Value, qMin, qMax);
            if (isoprofit.Count > 1)
            {
                list.Add(ChartSeriesBuilder.Line(Localization["Monopoly_Series_Isoprofit"], isoprofit, SKColors.MediumPurple));
            }
        }

        if (ShowAverageCost && cost != null)
        {
            var (qMin, qMax) = GetChartQRange();
            var averageCost = BuildAverageCostPoints(cost, qMin, qMax);
            if (averageCost.Count > 1)
            {
                list.Add(ChartSeriesBuilder.Line(Localization["Monopoly_Series_AverageCost"], averageCost, SKColors.CadetBlue));
            }
        }

        if (result.MonopolyPoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter(Localization["Monopoly_Series_MonopolyPoint"], result.MonopolyPoint.Value, SKColors.Firebrick));
        }

        if (result.CompetitivePoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter(Localization["Monopoly_Series_CompetitivePoint"], result.CompetitivePoint.Value, SKColors.DarkGreen));
        }

        if (ShowGuideLines && result.MonopolyPoint.HasValue)
        {
            var (qMin, qMax) = GetChartQRange();
            var (pMin, pMax) = GetChartPRange();
            var qm = result.MonopolyPoint.Value.X;
            var pm = result.MonopolyPoint.Value.Y;
            list.Add(ChartSeriesBuilder.VerticalLine(Localization["Monopoly_Series_QuantityGuide"], pMin, pMax, qm, SKColors.DimGray, true));
            list.Add(ChartSeriesBuilder.HorizontalLine(Localization["Monopoly_Series_PriceGuide"], qMin, qMax, pm, SKColors.DimGray, true));
        }

        if (ShowGuideLines && result.CompetitivePoint.HasValue)
        {
            var (pMin, pMax) = GetChartPRange();
            var qcp = result.CompetitivePoint.Value.X;
            list.Add(ChartSeriesBuilder.VerticalLine(Localization["Monopoly_Series_CompetitiveQuantityGuide"], pMin, pMax, qcp, SKColors.DarkSeaGreen, true));
        }

        if (ShowDeadweightArea && result.MonopolyPoint.HasValue && result.CompetitivePoint.HasValue)
        {
            var qm = result.MonopolyPoint.Value.X;
            var qcp = result.CompetitivePoint.Value.X;
            if (Math.Abs(qm - qcp) > 1e-3)
            {
                var start = Math.Min(qm, qcp);
                var end = Math.Max(qm, qcp);
                var samples = BuildAreaSamples(start, end, result.MarginalCost, result.Demand);
                foreach (var series in BuildDeadweightSeries(samples, SKColors.LightGreen.WithAlpha(110)))
                {
                    list.Add(series);
                }
            }
        }

        return list;
    }

    private (double min, double max) GetChartQRange()
    {
        var min = XAxes.Length > 0 ? XAxes[0].MinLimit ?? 0 : 0;
        var max = XAxes.Length > 0 ? XAxes[0].MaxLimit ?? 100 : 100;
        if (max <= min + 1e-3)
        {
            min = 0;
            max = 100;
        }

        return (Math.Max(0, min), max);
    }

    private (double min, double max) GetChartPRange()
    {
        var min = YAxes.Length > 0 ? YAxes[0].MinLimit ?? 0 : 0;
        var max = YAxes.Length > 0 ? YAxes[0].MaxLimit ?? 150 : 150;
        if (max <= min + 1e-3)
        {
            min = 0;
            max = 150;
        }

        return (Math.Max(0, min), max);
    }

    private static IReadOnlyList<ChartPoint> BuildIsoprofitPoints(OfertaDemanda.Core.Expressions.ParsedExpression cost, double profit, double qMin, double qMax)
    {
        var points = new List<ChartPoint>(IsoprofitSamples);
        var step = (qMax - qMin) / Math.Max(1, IsoprofitSamples - 1);

        for (var i = 0; i < IsoprofitSamples; i++)
        {
            var q = qMin + step * i;
            if (q <= IsoprofitEpsilon)
            {
                continue;
            }

            var costValue = NumericMethods.Safe(cost.Evaluate(q));
            var price = (profit + costValue) / q;
            if (double.IsNaN(price) || double.IsInfinity(price))
            {
                continue;
            }

            points.Add(new ChartPoint(q, price));
        }

        return points;
    }

    private static IReadOnlyList<AreaSamplePoint> BuildAreaSamples(double start, double end, IReadOnlyList<ChartPoint> baseSeries, IReadOnlyList<ChartPoint> topSeries)
    {
        if (baseSeries.Count == 0 || topSeries.Count == 0 || end <= start)
        {
            return Array.Empty<AreaSamplePoint>();
        }

        var samples = new AreaSamplePoint[60];
        for (var i = 0; i < samples.Length; i++)
        {
            var t = samples.Length == 1 ? 0 : (double)i / (samples.Length - 1);
            var q = start + (end - start) * t;
            var baseVal = NumericMethods.Safe(InterpolateSeries(baseSeries, q));
            var topVal = NumericMethods.Safe(InterpolateSeries(topSeries, q));
            var offset = Math.Max(0, topVal - baseVal);
            samples[i] = new AreaSamplePoint(q, baseVal, offset);
        }

        return samples;
    }

    private static double InterpolateSeries(IReadOnlyList<ChartPoint> series, double x)
    {
        if (series.Count == 0)
        {
            return double.NaN;
        }

        if (x <= series[0].X)
        {
            return series[0].Y;
        }

        for (var i = 1; i < series.Count; i++)
        {
            var prev = series[i - 1];
            var next = series[i];
            if (x <= next.X)
            {
                var span = next.X - prev.X;
                if (Math.Abs(span) < 1e-9)
                {
                    return next.Y;
                }

                var t = (x - prev.X) / span;
                return prev.Y + (next.Y - prev.Y) * t;
            }
        }

        return series[^1].Y;
    }

    private IEnumerable<ISeries> BuildDeadweightSeries(IReadOnlyList<AreaSamplePoint> samples, SKColor color)
    {
        if (samples.Count == 0)
        {
            return Array.Empty<ISeries>();
        }

        var topValues = samples.Select(p => new ObservablePoint(p.X, p.BaseValue + p.OffsetValue)).ToArray();
        var baseValues = samples.Select(p => new ObservablePoint(p.X, p.BaseValue)).ToArray();

        var fillSeries = new LineSeries<ObservablePoint>
        {
            Values = topValues,
            Fill = new SolidColorPaint(color),
            Stroke = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Name = Localization["Monopoly_Series_DeadweightLoss"],
            IsHoverable = false,
            Pivot = 0,
            ZIndex = 1
        };

        var maskSeries = new LineSeries<ObservablePoint>
        {
            Values = baseValues,
            Fill = new SolidColorPaint(GetMaskFillColor()),
            Stroke = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Name = string.Empty,
            IsHoverable = false,
            IsVisibleAtLegend = false,
            Pivot = 0,
            ZIndex = 2
        };

        return new ISeries[] { fillSeries, maskSeries };
    }

    private static IReadOnlyList<ChartPoint> BuildAverageCostPoints(OfertaDemanda.Core.Expressions.ParsedExpression cost, double qMin, double qMax)
    {
        var points = new List<ChartPoint>(AverageCostSamples);
        var step = (qMax - qMin) / Math.Max(1, AverageCostSamples - 1);

        for (var i = 0; i < AverageCostSamples; i++)
        {
            var q = qMin + step * i;
            if (q <= IsoprofitEpsilon)
            {
                continue;
            }

            var costValue = NumericMethods.Safe(cost.Evaluate(q));
            var averageCost = costValue / q;
            if (double.IsNaN(averageCost) || double.IsInfinity(averageCost))
            {
                continue;
            }

            points.Add(new ChartPoint(q, averageCost));
        }

        return points;
    }

    private static SKColor GetMaskFillColor()
    {
        var theme = Application.Current?.RequestedTheme;
        var isDark = theme == AppTheme.Dark;
        return isDark ? new SKColor(255, 255, 255, 60) : new SKColor(0, 0, 0, 40);
    }

    private string BuildIsoprofitExplanation(double? profit)
    {
        var profitValue = profit.HasValue
            ? profit.Value.ToString("F2", Localization.CurrentCulture)
            : Localization["Common_EmptyValue"];

        return string.Format(
            Localization.CurrentCulture,
            Localization["Monopoly_Isoprofit_Explanation"],
            profitValue);
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
    }

    private void OnLocalizationChanged()
    {
        UpdateAxisLabels();
        Recalculate();
    }
}
