using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Desktop.Services;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

public partial class IsoBenefitViewModel : ViewModelBase
{
    private const double MarketQuantityMax = 220;
    private const double FirmQuantityMax = 150;
    private readonly UserSettingsService _settingsService;
    private readonly double[] _firmProfitLevels = { -200, 0, 200 };
    private readonly double[] _marketProfitLevels = { -500, 0, 500 };
    private readonly RelayCommand _addFirmCommand;
    private readonly RelayCommand _removeFirmCommand;
    private bool _suppressUpdates;

    public IsoBenefitViewModel(UserSettingsService settingsService)
    {
        _settingsService = settingsService;
        Firms = new ObservableCollection<IsoBenefitFirmItemViewModel>();
        Firms.CollectionChanged += OnFirmsCollectionChanged;

        _addFirmCommand = new RelayCommand(AddFirmInternal);
        _removeFirmCommand = new RelayCommand(RemoveSelectedFirmInternal, () => SelectedFirm != null);

        MarketXAxes = new[]
        {
            new Axis { Name = "Q", MinLimit = 0, MaxLimit = 220 }
        };

        MarketYAxes = new[]
        {
            new Axis { Name = "P", MinLimit = 0, MaxLimit = 150 }
        };

        FirmXAxes = new[]
        {
            new Axis { Name = "q", MinLimit = 0, MaxLimit = 150 }
        };

        FirmYAxes = new[]
        {
            new Axis { Name = "p", MinLimit = 0, MaxLimit = 150 }
        };

        TheoryText = BuildTheoryText();
        LoadFromSettings();
        Recalculate();
    }

    public ObservableCollection<IsoBenefitFirmItemViewModel> Firms { get; }

    public Axis[] MarketXAxes { get; }
    public Axis[] MarketYAxes { get; }
    public Axis[] FirmXAxes { get; }
    public Axis[] FirmYAxes { get; }

    public string TheoryText { get; }

    public IRelayCommand AddFirmCommand => _addFirmCommand;
    public IRelayCommand RemoveFirmCommand => _removeFirmCommand;

    [ObservableProperty]
    private string demandExpression = AppDefaults.Market.DemandExpression;

    [ObservableProperty]
    private double demandShock;

    [ObservableProperty]
    private IsoBenefitFirmItemViewModel? selectedFirm;

    [ObservableProperty]
    private IEnumerable<ISeries> marketSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private IEnumerable<ISeries> firmSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private IReadOnlyList<string> errors = Array.Empty<string>();

    [ObservableProperty]
    private IReadOnlyList<FirmStatusViewModel> firmStatuses = Array.Empty<FirmStatusViewModel>();

    [ObservableProperty]
    private string referencePriceText = "P*: —";

    [ObservableProperty]
    private string referenceQuantityText = "Q*: —";

    [ObservableProperty]
    private string priceNoteText = string.Empty;

    public bool HasErrors => Errors.Count > 0;

    partial void OnSelectedFirmChanged(IsoBenefitFirmItemViewModel? value) => _removeFirmCommand.NotifyCanExecuteChanged();

    partial void OnDemandExpressionChanged(string value) => PersistAndRecalculate();

    partial void OnDemandShockChanged(double value) => PersistAndRecalculate();

    partial void OnErrorsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasErrors));

    private void LoadFromSettings()
    {
        var iso = _settingsService.Settings.IsoBenefit ?? IsoBenefitSettings.CreateDefault();
        _suppressUpdates = true;
        DemandExpression = iso.DemandExpression;
        DemandShock = iso.DemandShock;
        Firms.Clear();
        foreach (var firm in iso.Firms)
        {
            AddFirmFromSettings(firm.Name, firm.CostExpression);
        }

        if (Firms.Count == 0)
        {
            AddFirmFromSettings("Empresa A", AppDefaults.Firm.CostExpression);
        }

        SelectedFirm = Firms.FirstOrDefault();
        _suppressUpdates = false;
    }

    private void AddFirmFromSettings(string name, string costExpression)
    {
        var item = new IsoBenefitFirmItemViewModel
        {
            Name = name,
            CostExpression = costExpression
        };
        item.PropertyChanged += OnFirmPropertyChanged;
        Firms.Add(item);
    }

    private void OnFirmsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (IsoBenefitFirmItemViewModel firm in e.NewItems)
            {
                firm.PropertyChanged += OnFirmPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (IsoBenefitFirmItemViewModel firm in e.OldItems)
            {
                firm.PropertyChanged -= OnFirmPropertyChanged;
            }
        }

        PersistAndRecalculate();
    }

    private void OnFirmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IsoBenefitFirmItemViewModel.Name) or nameof(IsoBenefitFirmItemViewModel.CostExpression))
        {
            PersistAndRecalculate();
        }
    }

    private void AddFirmInternal()
    {
        var index = Firms.Count + 1;
        var name = $"Empresa {index}";
        var item = new IsoBenefitFirmItemViewModel
        {
            Name = name,
            CostExpression = AppDefaults.Firm.CostExpression
        };
        item.PropertyChanged += OnFirmPropertyChanged;
        Firms.Add(item);
        SelectedFirm = item;
    }

    private void RemoveSelectedFirmInternal()
    {
        if (SelectedFirm == null)
        {
            return;
        }

        Firms.Remove(SelectedFirm);
        SelectedFirm = Firms.FirstOrDefault();
    }

    private void PersistAndRecalculate()
    {
        if (_suppressUpdates)
        {
            return;
        }

        var updatedIso = (_settingsService.Settings.IsoBenefit ?? IsoBenefitSettings.CreateDefault()) with
        {
            DemandExpression = DemandExpression,
            DemandShock = DemandShock,
            Firms = Firms.Select(f => new IsoBenefitFirmSetting(f.Name, f.CostExpression)).ToArray()
        };

        var updatedSettings = _settingsService.Settings with { IsoBenefit = updatedIso };
        _settingsService.Update(updatedSettings);
        Recalculate();
    }

    private void Recalculate()
    {
        var errors = new List<string>();
        if (!TryParseExpression(DemandExpression, "Demanda inversa del mercado", errors, out var demandExpression))
        {
            UpdateState(null, errors);
            return;
        }

        var firmParameters = new List<IsoBenefitFirmParameters>();
        foreach (var firm in Firms)
        {
            if (!TryParseExpression(firm.CostExpression, $"Coste total ({firm.Name})", errors, out var parsedCost))
            {
                continue;
            }

            firmParameters.Add(new IsoBenefitFirmParameters(firm.Name, parsedCost!));
        }

        if (firmParameters.Count == 0)
        {
            errors.Add("Necesitas al menos una empresa con una función de costes válida.");
            UpdateState(null, errors);
            return;
        }

        try
        {
            var parameters = new IsoBenefitParameters(
                demandExpression!,
                DemandShock,
                firmParameters,
                _firmProfitLevels,
                _marketProfitLevels);

            var result = IsoBenefitCalculator.Calculate(parameters);
            UpdateState(result, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"No se pudo calcular la pestaña de isobeneficio: {ex.Message}");
            UpdateState(null, errors);
        }
    }

    private void UpdateState(IsoBenefitResult? result, List<string> errors)
    {
        if (result == null)
        {
            MarketSeries = Array.Empty<ISeries>();
            FirmSeries = Array.Empty<ISeries>();
            FirmStatuses = Array.Empty<FirmStatusViewModel>();
            ReferencePriceText = "P*: —";
            ReferenceQuantityText = "Q*: —";
            PriceNoteText = "Selecciona expresiones válidas para mostrar las curvas.";
            Errors = errors;
            return;
        }

        MarketSeries = BuildMarketSeries(result);
        FirmSeries = BuildFirmSeries(result);
        FirmStatuses = BuildFirmStatuses(result);
        ReferencePriceText = FormatMetric("P*", result.ReferencePrice, "F2");
        ReferenceQuantityText = FormatMetric("Q*", result.ReferenceQuantity, "F2");
        PriceNoteText = result.UsedFallbackPrice
            ? "Se usó P*=40 como referencia al no converger el equilibrio competitivo."
            : "P* se obtiene resolviendo P_d(Q_s(P)) = P con Q_s(P)=Σq_i(P).";
        Errors = errors.Count == 0 ? Array.Empty<string>() : errors;
    }

    private IEnumerable<ISeries> BuildMarketSeries(IsoBenefitResult result)
    {
        var series = new List<ISeries>
        {
            ChartSeriesBuilder.Line("Demanda P_d(Q)", result.Market.Demand, SKColors.SlateGray)
        };

        foreach (var curve in result.Market.Curves)
        {
            var color = curve.TargetProfit switch
            {
                < 0 => SKColors.LightCoral,
                0 => SKColors.Gray,
                _ => SKColors.OliveDrab
            };

            series.Add(ChartSeriesBuilder.Line($"Π̄ = {curve.TargetProfit:F0}", curve.Points, color, curve.TargetProfit < 0));
            if (curve.Intersection.HasValue)
            {
                series.Add(ChartSeriesBuilder.Scatter($"Intersección Π̄={curve.TargetProfit:F0}", curve.Intersection.Value, color));
            }
        }

        series.Add(ChartSeriesBuilder.HorizontalLine("Precio P*", 0, MarketQuantityMax, result.Market.ReferencePrice, SKColors.Firebrick, dashed: true));

        if (result.Market.ReferencePoint.HasValue)
        {
            series.Add(ChartSeriesBuilder.Scatter("Referencia competitiva", result.Market.ReferencePoint.Value, SKColors.Firebrick));
        }

        return series;
    }

    private IEnumerable<ISeries> BuildFirmSeries(IsoBenefitResult result)
    {
        var series = new List<ISeries>
        {
            ChartSeriesBuilder.HorizontalLine("Precio P*", 0, FirmQuantityMax, result.ReferencePrice, SKColors.Firebrick, dashed: true)
        };

        for (var i = 0; i < result.Firms.Count; i++)
        {
            var firm = result.Firms[i];
            var color = GetFirmColor(i);
            foreach (var curve in firm.Curves)
            {
                var label = $"{firm.Name} π̄={curve.TargetProfit:F0}";
                series.Add(ChartSeriesBuilder.Line(label, curve.Points, color, curve.TargetProfit < 0));
                if (curve.Intersection.HasValue)
                {
                    series.Add(ChartSeriesBuilder.Scatter($"{firm.Name} intersección", curve.Intersection.Value, color));
                }
            }

            if (firm.OptimalPoint.HasValue)
            {
                series.Add(ChartSeriesBuilder.Scatter($"{firm.Name} q*", firm.OptimalPoint.Value, SKColors.Black));
            }
        }

        return series;
    }

    private IReadOnlyList<FirmStatusViewModel> BuildFirmStatuses(IsoBenefitResult result)
    {
        var list = new List<FirmStatusViewModel>(result.Firms.Count);
        foreach (var firm in result.Firms)
        {
            var status = firm.Status switch
            {
                IsoProfitStatus.Positive => "Gana dinero",
                IsoProfitStatus.Negative => "Pierde dinero",
                _ => "Beneficio cero"
            };

            var profitText = $"π* = {firm.OptimalProfit:F2}";
            var quantityText = firm.OptimalQuantity > 0 ? $"q* = {firm.OptimalQuantity:F2}" : "q* = 0";
            list.Add(new FirmStatusViewModel(firm.Name, status, $"{profitText} · {quantityText}"));
        }

        return list;
    }

    private static string BuildTheoryText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Beneficio de cada empresa: π_i(q,p)=p·q−C_i(q).");
        builder.AppendLine("Curvas de isobeneficio: p(q;π̄_i)=(C_i(q)+π̄_i)/q para q>0 con niveles π̄_i negativos, nulos y positivos.");
        builder.AppendLine("En competencia perfecta el precio es horizontal p=P* y la empresa produce donde CMg_i(q)=P*, con la regla de cierre P*<CMeV_i(q).");
        builder.AppendLine("La etiqueta Gana/Pierde/Cero se evalúa con π_i*=P*·q_i*−C_i(q_i*).");
        builder.AppendLine("Para el mercado agregamos costes suponiendo reparto uniforme q_i=Q/N, de modo que C_M(Q)=Σ C_i(Q/N) y Π(Q,P)=P·Q−C_M(Q).");
        builder.AppendLine("Las curvas Π̄ del mercado se calculan como P(Q;Π̄)=(C_M(Q)+Π̄)/Q evitando Q=0 y se cruzan con la demanda P_d(Q).");
        builder.AppendLine("Determinamos P* resolviendo numéricamente P_d(Q_s(P))=P donde Q_s(P)=Σ q_i(P); si no hay solución estable, usamos P*=40 como referencia.");
        return builder.ToString();
    }

    private static SKColor GetFirmColor(int index)
    {
        var palette = new[]
        {
            SKColors.SteelBlue,
            SKColors.SeaGreen,
            SKColors.MediumOrchid,
            SKColors.DarkOrange,
            SKColors.SaddleBrown
        };

        return palette[index % palette.Length];
    }

    public sealed record FirmStatusViewModel(string Name, string Summary, string Details);
}
