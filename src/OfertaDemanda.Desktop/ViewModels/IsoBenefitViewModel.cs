using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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

    public IsoBenefitViewModel(UserSettingsService settingsService, LocalizationService localization)
        : base(localization)
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

        Localization.CultureChanged += (_, _) => OnLocalizationChanged();
        UpdateAxisLabels();
        TheoryText = Localization["IsoBenefit_TheoryText"];
        LoadFromSettings();
        UpdateDemandShockText();
        Recalculate();
    }

    public ObservableCollection<IsoBenefitFirmItemViewModel> Firms { get; }

    public Axis[] MarketXAxes { get; }
    public Axis[] MarketYAxes { get; }
    public Axis[] FirmXAxes { get; }
    public Axis[] FirmYAxes { get; }

    [ObservableProperty]
    private string theoryText = string.Empty;

    public IRelayCommand AddFirmCommand => _addFirmCommand;
    public IRelayCommand RemoveFirmCommand => _removeFirmCommand;

    [ObservableProperty]
    private string demandExpression = AppDefaults.Market.DemandExpression;

    [ObservableProperty]
    private double demandShock;

    [ObservableProperty]
    private string demandShockText = string.Empty;

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
    private string referencePriceText = string.Empty;

    [ObservableProperty]
    private string referenceQuantityText = string.Empty;

    [ObservableProperty]
    private string priceNoteText = string.Empty;

    public bool HasErrors => Errors.Count > 0;

    partial void OnSelectedFirmChanged(IsoBenefitFirmItemViewModel? value) => _removeFirmCommand.NotifyCanExecuteChanged();

    partial void OnDemandExpressionChanged(string value) => PersistAndRecalculate();

    partial void OnDemandShockChanged(double value)
    {
        UpdateDemandShockText();
        PersistAndRecalculate();
    }

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
            AddFirmFromSettings(string.Format(Localization.CurrentCulture, Localization["IsoBenefit_DefaultFirmNameFormat"], 1), AppDefaults.Firm.CostExpression);
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
        var name = string.Format(Localization.CurrentCulture, Localization["IsoBenefit_DefaultFirmNameFormat"], index);
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
        if (!TryParseExpression(DemandExpression, Localization["IsoBenefit_Parse_MarketDemand"], errors, out var demandExpression))
        {
            UpdateState(null, errors);
            return;
        }

        var firmParameters = new List<IsoBenefitFirmParameters>();
        foreach (var firm in Firms)
        {
            if (!TryParseExpression(firm.CostExpression, string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Parse_FirmCost"], firm.Name), errors, out var parsedCost))
            {
                continue;
            }

            firmParameters.Add(new IsoBenefitFirmParameters(firm.Name, parsedCost!));
        }

        if (firmParameters.Count == 0)
        {
            errors.Add(Localization["IsoBenefit_Error_NoFirms"]);
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
            errors.Add(string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Error_CalcFailed"], ex.Message));
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
            ReferencePriceText = FormatMetric("IsoBenefit_Label_ReferencePrice", null);
            ReferenceQuantityText = FormatMetric("IsoBenefit_Label_ReferenceQuantity", null);
            PriceNoteText = Localization["IsoBenefit_PriceNote_Invalid"];
            Errors = errors;
            return;
        }

        MarketSeries = BuildMarketSeries(result);
        FirmSeries = BuildFirmSeries(result);
        FirmStatuses = BuildFirmStatuses(result);
        ReferencePriceText = FormatMetric("IsoBenefit_Label_ReferencePrice", result.ReferencePrice, "F2");
        ReferenceQuantityText = FormatMetric("IsoBenefit_Label_ReferenceQuantity", result.ReferenceQuantity, "F2");
        PriceNoteText = result.UsedFallbackPrice
            ? Localization["IsoBenefit_PriceNote_Fallback"]
            : Localization["IsoBenefit_PriceNote_Normal"];
        Errors = errors.Count == 0 ? Array.Empty<string>() : errors;
    }

    private IEnumerable<ISeries> BuildMarketSeries(IsoBenefitResult result)
    {
        var series = new List<ISeries>
        {
            ChartSeriesBuilder.Line(Localization["IsoBenefit_Series_MarketDemand"], result.Market.Demand, SKColors.SlateGray)
        };

        foreach (var curve in result.Market.Curves)
        {
            var color = curve.TargetProfit switch
            {
                < 0 => SKColors.LightCoral,
                0 => SKColors.Gray,
                _ => SKColors.OliveDrab
            };

            var levelLabel = string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Series_ProfitLevel"], curve.TargetProfit);
            series.Add(ChartSeriesBuilder.Line(levelLabel, curve.Points, color, curve.TargetProfit < 0));
            if (curve.Intersection.HasValue)
            {
                var intersectionLabel = string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Series_ProfitIntersection"], curve.TargetProfit);
                series.Add(ChartSeriesBuilder.Scatter(intersectionLabel, curve.Intersection.Value, color));
            }
        }

        series.Add(ChartSeriesBuilder.HorizontalLine(Localization["IsoBenefit_Series_PriceLine"], 0, MarketQuantityMax, result.Market.ReferencePrice, SKColors.Firebrick, dashed: true));

        if (result.Market.ReferencePoint.HasValue)
        {
            series.Add(ChartSeriesBuilder.Scatter(Localization["IsoBenefit_Series_CompetitiveReference"], result.Market.ReferencePoint.Value, SKColors.Firebrick));
        }

        return series;
    }

    private IEnumerable<ISeries> BuildFirmSeries(IsoBenefitResult result)
    {
        var series = new List<ISeries>
        {
            ChartSeriesBuilder.HorizontalLine(Localization["IsoBenefit_Series_PriceLine"], 0, FirmQuantityMax, result.ReferencePrice, SKColors.Firebrick, dashed: true)
        };

        for (var i = 0; i < result.Firms.Count; i++)
        {
            var firm = result.Firms[i];
            var color = GetFirmColor(i);
            foreach (var curve in firm.Curves)
            {
                var label = string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Series_FirmProfitLevel"], firm.Name, curve.TargetProfit);
                series.Add(ChartSeriesBuilder.Line(label, curve.Points, color, curve.TargetProfit < 0));
                if (curve.Intersection.HasValue)
                {
                    var intersectionLabel = string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Series_FirmIntersection"], firm.Name);
                    series.Add(ChartSeriesBuilder.Scatter(intersectionLabel, curve.Intersection.Value, color));
                }
            }

            if (firm.OptimalPoint.HasValue)
            {
                var optimalLabel = string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Series_FirmOptimal"], firm.Name);
                series.Add(ChartSeriesBuilder.Scatter(optimalLabel, firm.OptimalPoint.Value, SKColors.Black));
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
                IsoProfitStatus.Positive => Localization["IsoBenefit_Status_Positive"],
                IsoProfitStatus.Negative => Localization["IsoBenefit_Status_Negative"],
                _ => Localization["IsoBenefit_Status_Zero"]
            };

            var profitText = string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Status_ProfitFormat"], firm.OptimalProfit);
            var quantityText = firm.OptimalQuantity > 0
                ? string.Format(Localization.CurrentCulture, Localization["IsoBenefit_Status_QuantityFormat"], firm.OptimalQuantity)
                : Localization["IsoBenefit_Status_QuantityZero"];
            list.Add(new FirmStatusViewModel(firm.Name, status, $"{profitText} · {quantityText}"));
        }

        return list;
    }

    private void UpdateAxisLabels()
    {
        if (MarketXAxes.Length > 0)
        {
            MarketXAxes[0].Name = Localization["Axis_Quantity"];
        }

        if (MarketYAxes.Length > 0)
        {
            MarketYAxes[0].Name = Localization["Axis_Price"];
        }

        if (FirmXAxes.Length > 0)
        {
            FirmXAxes[0].Name = Localization["Axis_QuantityLower"];
        }

        if (FirmYAxes.Length > 0)
        {
            FirmYAxes[0].Name = Localization["Axis_PriceLower"];
        }
    }

    private void UpdateDemandShockText()
    {
        DemandShockText = string.Format(Localization.CurrentCulture, Localization["Format_CurrentValue"], DemandShock);
    }

    private void OnLocalizationChanged()
    {
        UpdateAxisLabels();
        TheoryText = Localization["IsoBenefit_TheoryText"];
        UpdateDemandShockText();
        Recalculate();
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
