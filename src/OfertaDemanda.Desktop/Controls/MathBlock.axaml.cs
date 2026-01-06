using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using OfertaDemanda.Shared.Math;

namespace OfertaDemanda.Desktop.Controls;

public partial class MathBlock : UserControl
{
    private const double LayoutEpsilon = 0.5;
    public static readonly StyledProperty<string?> LatexProperty =
        AvaloniaProperty.Register<MathBlock, string?>(nameof(Latex));

    public new static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<MathBlock, double>(nameof(FontSize), 16);

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<MathBlock, string?>(nameof(Hint));

    public static readonly StyledProperty<bool> InlineProperty =
        AvaloniaProperty.Register<MathBlock, bool>(nameof(Inline), false);

    public static IMathFormulaRenderer? DefaultRenderer { get; set; }
    private string? _lastLatex;
    private double _lastFontSize;
    private MathTheme _lastTheme;
    private double _lastScale;
    private double _lastAvailableWidth;
    private double _lastAvailableHeight;

    public string? Latex
    {
        get => GetValue(LatexProperty);
        set => SetValue(LatexProperty, value);
    }

    public new double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public bool Inline
    {
        get => GetValue(InlineProperty);
        set => SetValue(InlineProperty, value);
    }

    public MathBlock()
    {
        InitializeComponent();
        PropertyChanged += OnPropertyChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current is Application app)
        {
            app.ActualThemeVariantChanged += OnThemeChanged;
        }
        RenderFormula();
        UpdateHint();
        UpdateOrientation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Application.Current is Application app)
        {
            app.ActualThemeVariantChanged -= OnThemeChanged;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        RenderFormula();
    }

    private void UpdateHint()
    {
        if (HintText == null)
        {
            return;
        }

        HintText.Text = Hint ?? string.Empty;
        HintText.IsVisible = !string.IsNullOrWhiteSpace(Hint);
    }

    private void UpdateOrientation()
    {
        if (RootPanel == null)
        {
            return;
        }

        RootPanel.Orientation = Inline ? Orientation.Horizontal : Orientation.Vertical;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == LatexProperty || e.Property == FontSizeProperty)
        {
            RenderFormula();
            return;
        }

        if (e.Property == BoundsProperty || e.Property == MaxWidthProperty || e.Property == MaxHeightProperty)
        {
            RenderFormula();
            return;
        }

        if (e.Property == HintProperty)
        {
            UpdateHint();
            return;
        }

        if (e.Property == InlineProperty)
        {
            UpdateOrientation();
        }
    }

    private void RenderFormula()
    {
        if (FormulaImage == null)
        {
            return;
        }

        var latex = Latex;
        if (string.IsNullOrWhiteSpace(latex))
        {
            FormulaImage.Source = null;
            return;
        }

        var renderer = DefaultRenderer ?? new CSharpMathFormulaRenderer();
        var theme = (Application.Current?.ActualThemeVariant ?? ThemeVariant.Default) == ThemeVariant.Dark
            ? MathTheme.Dark
            : MathTheme.Light;
        var scale = (float)(VisualRoot?.RenderScaling ?? 1d);
        var availableWidth = MaxWidth;
        if (double.IsInfinity(availableWidth) || availableWidth <= 0)
        {
            availableWidth = Bounds.Width;
        }

        var availableHeight = MaxHeight;
        if (double.IsInfinity(availableHeight) || availableHeight <= 0)
        {
            availableHeight = Bounds.Height;
        }
        if (availableWidth <= 0)
        {
            availableWidth = double.PositiveInfinity;
        }
        if (availableHeight <= 0)
        {
            availableHeight = double.PositiveInfinity;
        }

        if (string.Equals(_lastLatex, latex, StringComparison.Ordinal) &&
            Math.Abs(_lastFontSize - FontSize) < LayoutEpsilon &&
            _lastTheme == theme &&
            Math.Abs(_lastScale - scale) < LayoutEpsilon &&
            Math.Abs(_lastAvailableWidth - availableWidth) < LayoutEpsilon &&
            Math.Abs(_lastAvailableHeight - availableHeight) < LayoutEpsilon)
        {
            return;
        }

        var targetFontSize = (float)FontSize;
        var result = renderer.Render(latex, targetFontSize, theme, scale);
        if (result.PngBytes.Length == 0)
        {
            FormulaImage.Source = null;
            return;
        }

        var visualWidth = result.Width / scale;
        if (availableWidth < double.PositiveInfinity && visualWidth > availableWidth)
        {
            var ratio = (float)(availableWidth / visualWidth);
            targetFontSize = (float)Math.Max(8d, FontSize * ratio);
            result = renderer.Render(latex, targetFontSize, theme, scale);
            if (result.PngBytes.Length == 0)
            {
                FormulaImage.Source = null;
                return;
            }
        }

        using var stream = new MemoryStream(result.PngBytes);
        FormulaImage.Source = new Bitmap(stream);
        if (availableWidth < double.PositiveInfinity)
        {
            FormulaImage.Width = availableWidth;
        }
        else
        {
            FormulaImage.Width = double.NaN;
        }

        FormulaImage.Height = double.NaN;

        _lastLatex = latex;
        _lastFontSize = FontSize;
        _lastTheme = theme;
        _lastScale = scale;
        _lastAvailableWidth = availableWidth;
        _lastAvailableHeight = availableHeight;
    }
}
