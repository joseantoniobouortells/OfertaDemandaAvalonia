using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using OfertaDemanda.Core.Models;
using SkiaSharp;

namespace OfertaDemanda.Desktop.ViewModels;

internal static class ChartSeriesBuilder
{
    public static ISeries Line(string name, IReadOnlyList<ChartPoint> data, SKColor color, bool dashed = false)
    {
        var line = new LineSeries<ObservablePoint>
        {
            Name = name,
            LineSmoothness = 0,
            GeometryFill = null,
            GeometryStroke = null,
            Fill = null,
            Values = ToObservablePoints(data),
            Stroke = CreateStroke(color, dashed)
        };

        return line;
    }

    public static ISeries HorizontalLine(string name, double xStart, double xEnd, double value, SKColor color, bool dashed = false)
    {
        var points = new[]
        {
            new ChartPoint(xStart, value),
            new ChartPoint(xEnd, value)
        };

        return Line(name, points, color, dashed);
    }

    public static ISeries Scatter(string name, ChartPoint point, SKColor color, double size = 14)
    {
        return new ScatterSeries<ObservablePoint>
        {
            Name = name,
            GeometrySize = size,
            Values = new[] { new ObservablePoint(point.X, point.Y) },
            Fill = new SolidColorPaint(color),
            Stroke = null
        };
    }

    private static ObservablePoint[] ToObservablePoints(IReadOnlyList<ChartPoint> data)
    {
        var result = new ObservablePoint[data.Count];
        for (var i = 0; i < data.Count; i++)
        {
            result[i] = new ObservablePoint(data[i].X, data[i].Y);
        }

        return result;
    }

    private static SolidColorPaint CreateStroke(SKColor color, bool dashed)
    {
        return new SolidColorPaint(color)
        {
            StrokeThickness = dashed ? 1.5f : 2f
        };
    }
}
