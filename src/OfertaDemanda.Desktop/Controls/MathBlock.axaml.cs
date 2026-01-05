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
    public static readonly StyledProperty<string?> LatexProperty =
        AvaloniaProperty.Register<MathBlock, string?>(nameof(Latex));

    public new static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<MathBlock, double>(nameof(FontSize), 16);

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<MathBlock, string?>(nameof(Hint));

    public static readonly StyledProperty<bool> InlineProperty =
        AvaloniaProperty.Register<MathBlock, bool>(nameof(Inline), false);

    public static IMathFormulaRenderer? DefaultRenderer { get; set; }

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

        var result = renderer.Render(latex, (float)FontSize, theme, scale);
        if (result.PngBytes.Length == 0)
        {
            FormulaImage.Source = null;
            return;
        }

        using var stream = new MemoryStream(result.PngBytes);
        FormulaImage.Source = new Bitmap(stream);
    }
}
