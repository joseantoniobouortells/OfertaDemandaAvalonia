using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OfertaDemanda.Desktop.ViewModels;

public sealed partial class ChartSeriesToggle : ObservableObject
{
    public ChartSeriesToggle(string key, string label, string group, bool isVisible)
    {
        Key = key;
        Label = label;
        Group = group;
        this.isVisible = isVisible;
    }

    public string Key { get; }
    public string Label { get; }
    public string Group { get; }

    [ObservableProperty]
    private bool isVisible;
}

public sealed class ChartSeriesToggleGroup
{
    public ChartSeriesToggleGroup(string label, IEnumerable<ChartSeriesToggle> items)
    {
        Label = label;
        Items = new ObservableCollection<ChartSeriesToggle>(items);
    }

    public string Label { get; }
    public ObservableCollection<ChartSeriesToggle> Items { get; }
}
