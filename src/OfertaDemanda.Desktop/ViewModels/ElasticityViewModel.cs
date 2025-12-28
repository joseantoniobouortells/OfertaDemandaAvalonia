using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using OfertaDemanda.Core.Models;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class ElasticityViewModel : ViewModelBase
{
    private bool _suppressUpdates;
    private readonly MarketViewModel _market;

    [ObservableProperty]
    private double price = 50;

    [ObservableProperty]
    private IEnumerable<ISeries> series = Array.Empty<ISeries>();

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    public bool HasErrors => Errors.Count > 0;

    [ObservableProperty]
    private string elasticityText = "Elasticidad: —";

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "q", MinLimit = 0, MaxLimit = 150 }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "P", MinLimit = 0, MaxLimit = 150 }
    };

    public MarketViewModel Market => _market;

    public ElasticityViewModel(MarketViewModel market)
    {
        _market = market;
        _market.PropertyChanged += OnMarketPropertyChanged;
        ApplyDefaults();
    }

    public void ApplyDefaults()
    {
        _suppressUpdates = true;
        Price = AppDefaults.Elasticity.Price;
        _suppressUpdates = false;
        Recalculate();
    }

    partial void OnPriceChanged(double value)
    {
        if (!_suppressUpdates) Recalculate();
    }

    private void Recalculate()
    {
        var localErrors = new List<string>();
        if (!TryParseExpression(_market.DemandExpression, "Demanda inversa (mercado)", localErrors, out var demand))
        {
            UpdateState(null, localErrors);
            return;
        }

        var result = ElasticityCalculator.Calculate(new ElasticityParameters(demand!, _market.DemandShock, Price));
        if (result.Errors.Count > 0)
        {
            localErrors.AddRange(result.Errors);
        }

        UpdateState(result, localErrors);
    }

    private void UpdateState(ElasticityResult? result, List<string> localErrors)
    {
        if (result == null)
        {
            Series = Array.Empty<ISeries>();
            ElasticityText = "Elasticidad: —";
        }
        else
        {
            Series = BuildSeries(result);
            ElasticityText = FormatMetric("Elasticidad", result.Elasticity);
        }

        Errors = localErrors.Count == 0 ? Array.Empty<string>() : localErrors.ToArray();
    }

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private IEnumerable<ISeries> BuildSeries(ElasticityResult result)
    {
        var list = new List<ISeries>
        {
            ChartSeriesBuilder.Line("Pd", result.Demand, SKColors.SteelBlue)
        };

        if (result.Point.HasValue)
        {
            list.Add(ChartSeriesBuilder.Scatter("Precio", result.Point.Value, SKColors.Firebrick));
        }

        return list;
    }

    private void OnMarketPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarketViewModel.DemandExpression) or nameof(MarketViewModel.DemandShock))
        {
            Recalculate();
        }
    }
}
