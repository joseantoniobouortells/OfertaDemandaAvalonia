using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Core.Numerics;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class MonopolyViewModel : ViewModelBase
{
    private const int IsoprofitSamples = 140;
    private const double IsoprofitEpsilon = 0.1;

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
    private string monopolyQuantityText = "q_m: —";

    [ObservableProperty]
    private string monopolyPriceText = "p_m: —";

    [ObservableProperty]
    private string profitText = "Beneficio: —";

    [ObservableProperty]
    private string competitiveQuantityText = "q_CP: —";

    [ObservableProperty]
    private string deadweightLossText = "Pérdida irrecup.: —";

    [ObservableProperty]
    private string isoprofitExplanation = "La isobeneficio muestra combinaciones (q, p) con π constante. πₘ: —.";

    [ObservableProperty]
    private bool showIsoprofitCurve = true;

    [ObservableProperty]
    private bool showGuideLines = true;

    [ObservableProperty]
    private bool showDeadweightArea = true;

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "q", MinLimit = 0, MaxLimit = 100 }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "P", MinLimit = 0, MaxLimit = 150 }
    };

    public MonopolyViewModel() => ApplyDefaults();

    public void ApplyDefaults()
    {
        _suppressUpdates = true;
        DemandExpression = AppDefaults.Monopoly.DemandExpression;
        CostExpression = AppDefaults.Monopoly.CostExpression;
        ShowIsoprofitCurve = true;
        ShowGuideLines = true;
        ShowDeadweightArea = true;
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

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(DemandExpression, "Pd(q)", localErrors, out var demand) ||
            !TryParseExpression(CostExpression, "CT(q)", localErrors, out var cost))
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
            MonopolyQuantityText = "q_m: —";
            MonopolyPriceText = "p_m: —";
            ProfitText = "Beneficio: —";
            CompetitiveQuantityText = "q_CP: —";
            DeadweightLossText = "Pérdida irrecup.: —";
            IsoprofitExplanation = "La isobeneficio muestra combinaciones (q, p) con π constante. πₘ: —.";
        }
        else
        {
            Series = BuildSeries(result, cost);
            MonopolyQuantityText = FormatMetric("q_m", result.MonopolyPoint?.X);
            MonopolyPriceText = FormatMetric("p_m", result.MonopolyPoint?.Y);
            ProfitText = FormatMetric("Beneficio", result.Profit);
            CompetitiveQuantityText = FormatMetric("q_CP", result.CompetitivePoint?.X);
            DeadweightLossText = FormatMetric("Pérdida irrecup.", result.DeadweightLoss);
            IsoprofitExplanation = BuildIsoprofitExplanation(result.Profit);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(MonopolyResult result, OfertaDemanda.Core.Expressions.ParsedExpression? cost)
    {
        var list = new List<ISeries>
        {
            ChartSeriesBuilder.Line("Pd", result.Demand, SKColors.SteelBlue),
            ChartSeriesBuilder.Line("IMg", result.MarginalRevenue, SKColors.DarkOrange),
            ChartSeriesBuilder.Line("CMg", result.MarginalCost, SKColors.OliveDrab)
        };

        if (ShowIsoprofitCurve && cost != null && result.MonopolyPoint.HasValue && result.Profit.HasValue)
        {
            var (qMin, qMax) = GetChartQRange();
            var isoprofit = BuildIsoprofitPoints(cost, result.Profit.Value, qMin, qMax);
            if (isoprofit.Count > 1)
            {
                list.Add(ChartSeriesBuilder.Line("Isobeneficio (π=π*)", isoprofit, SKColors.MediumPurple));
            }
        }

        if (result.MonopolyPoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter("Monopolio", result.MonopolyPoint.Value, SKColors.Firebrick));
        }

        if (result.CompetitivePoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter("CP", result.CompetitivePoint.Value, SKColors.DarkGreen));
        }

        if (ShowGuideLines && result.MonopolyPoint.HasValue)
        {
            var (qMin, qMax) = GetChartQRange();
            var (pMin, pMax) = GetChartPRange();
            var qm = result.MonopolyPoint.Value.X;
            var pm = result.MonopolyPoint.Value.Y;
            list.Add(ChartSeriesBuilder.VerticalLine("qₘ", pMin, pMax, qm, SKColors.DimGray, true));
            list.Add(ChartSeriesBuilder.HorizontalLine("pₘ", qMin, qMax, pm, SKColors.DimGray, true));
        }

        if (ShowGuideLines && result.CompetitivePoint.HasValue)
        {
            var (pMin, pMax) = GetChartPRange();
            var qcp = result.CompetitivePoint.Value.X;
            list.Add(ChartSeriesBuilder.VerticalLine("q_CP", pMin, pMax, qcp, SKColors.DarkSeaGreen, true));
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

    private static IEnumerable<ISeries> BuildDeadweightSeries(IReadOnlyList<AreaSamplePoint> samples, SKColor color)
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
            Name = "Pérdida irrecup.",
            IsHoverable = false,
            Pivot = 0,
            ZIndex = 1
        };

        var maskSeries = new LineSeries<ObservablePoint>
        {
            Values = baseValues,
            Fill = new SolidColorPaint(SKColors.White),
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

    private static string BuildIsoprofitExplanation(double? profit)
    {
        var profitValue = profit.HasValue
            ? profit.Value.ToString("F2", CultureInfo.InvariantCulture)
            : "—";

        return "La isobeneficio recoge combinaciones (q, p) con π constante. " +
               "En el óptimo monopolista, la curva con π=πₘ es tangente a Pd(q) porque IMg(qₘ)=CMg(qₘ). " +
               $"πₘ = {profitValue}.";
    }
}
