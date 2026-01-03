using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OfertaDemanda.Desktop.ViewModels;

public sealed partial class ChartSeriesToggle : ObservableObject
{
    public ChartSeriesToggle(string key, string labelKey, string groupKey, bool isVisible)
    {
        Key = key;
        LabelKey = labelKey;
        GroupKey = groupKey;
        label = labelKey;
        this.isVisible = isVisible;
    }

    public string Key { get; }
    public string LabelKey { get; }
    public string GroupKey { get; }

    [ObservableProperty]
    private string label;

    [ObservableProperty]
    private bool isVisible;
}

public sealed partial class ChartSeriesToggleGroup : ObservableObject
{
    public ChartSeriesToggleGroup(string labelKey, IEnumerable<ChartSeriesToggle> items)
    {
        LabelKey = labelKey;
        label = labelKey;
        Items = new ObservableCollection<ChartSeriesToggle>(items);
    }

    public string LabelKey { get; }

    [ObservableProperty]
    private string label;

    public ObservableCollection<ChartSeriesToggle> Items { get; }
}
