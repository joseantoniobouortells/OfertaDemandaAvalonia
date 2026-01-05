using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using CSharpMath.SkiaSharp;
using SkiaSharp;

namespace OfertaDemanda.Shared.Math;

public sealed class CSharpMathFormulaRenderer : IMathFormulaRenderer
{
    private readonly Dictionary<string, MathRenderResult> _cache = new();
    private readonly object _lock = new();

    public MathRenderResult Render(string latex, float fontSize, MathTheme theme, float dpiScale)
    {
        if (string.IsNullOrWhiteSpace(latex))
        {
            return new MathRenderResult(Array.Empty<byte>(), 0, 0);
        }

        var key = $"{latex}|{fontSize:F2}|{theme}|{dpiScale:F2}";
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var color = theme == MathTheme.Dark ? SKColors.White : SKColors.Black;
        var painter = new MathPainter
        {
            LaTeX = latex,
            FontSize = fontSize,
            TextColor = color,
            Magnification = System.Math.Max(1f, dpiScale),
            AntiAlias = true
        };

        var rect = painter.Measure(0);
        var width = (int)System.Math.Ceiling(rect.Width);
        var height = (int)System.Math.Ceiling(rect.Height);
        if (width <= 0 || height <= 0)
        {
            return new MathRenderResult(Array.Empty<byte>(), 0, 0);
        }

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        painter.Draw(canvas, -rect.Left, -rect.Top);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = data.ToArray();
        var result = new MathRenderResult(bytes, bitmap.Width, bitmap.Height);

        lock (_lock)
        {
            _cache[key] = result;
        }

        return result;
    }

    public void ClearCache()
    {
        lock (_lock)
        {
            _cache.Clear();
        }
    }
}
