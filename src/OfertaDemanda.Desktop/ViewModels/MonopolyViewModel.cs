using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using OfertaDemanda.Core.Models;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class MonopolyViewModel : ViewModelBase
{
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

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(DemandExpression, "Pd(q)", localErrors, out var demand) ||
            !TryParseExpression(CostExpression, "CT(q)", localErrors, out var cost))
        {
            UpdateState(null, localErrors);
            return;
        }

        var result = MonopolyCalculator.Calculate(new MonopolyParameters(demand!, cost!));
        if (result.Errors.Count > 0)
        {
            localErrors.AddRange(result.Errors);
        }

        UpdateState(result, localErrors);
    }

    private void UpdateState(MonopolyResult? result, List<string> localErrors)
    {
        if (result == null)
        {
            Series = Array.Empty<ISeries>();
            MonopolyQuantityText = "q_m: —";
            MonopolyPriceText = "p_m: —";
            ProfitText = "Beneficio: —";
            CompetitiveQuantityText = "q_CP: —";
            DeadweightLossText = "Pérdida irrecup.: —";
        }
        else
        {
            Series = BuildSeries(result);
            MonopolyQuantityText = FormatMetric("q_m", result.MonopolyPoint?.X);
            MonopolyPriceText = FormatMetric("p_m", result.MonopolyPoint?.Y);
            ProfitText = FormatMetric("Beneficio", result.Profit);
            CompetitiveQuantityText = FormatMetric("q_CP", result.CompetitivePoint?.X);
            DeadweightLossText = FormatMetric("Pérdida irrecup.", result.DeadweightLoss);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(MonopolyResult result)
    {
        var list = new List<ISeries>
        {
            ChartSeriesBuilder.Line("Pd", result.Demand, SKColors.SteelBlue),
            ChartSeriesBuilder.Line("IMg", result.MarginalRevenue, SKColors.DarkOrange),
            ChartSeriesBuilder.Line("CMg", result.MarginalCost, SKColors.OliveDrab)
        };

        if (result.MonopolyPoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter("Monopolio", result.MonopolyPoint.Value, SKColors.Firebrick));
        }

        if (result.CompetitivePoint.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter("CP", result.CompetitivePoint.Value, SKColors.DarkGreen));
        }

        return list;
    }
}
