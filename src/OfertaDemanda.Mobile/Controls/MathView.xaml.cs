using System;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using OfertaDemanda.Shared.Math;

namespace OfertaDemanda.Mobile.Controls;

public partial class MathView : ContentView
{
    public static readonly BindableProperty LatexProperty =
        BindableProperty.Create(nameof(Latex), typeof(string), typeof(MathView), default(string), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(MathView), 16d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(MathView), default(string), propertyChanged: OnHintChanged);

    public static readonly BindableProperty InlineProperty =
        BindableProperty.Create(nameof(Inline), typeof(bool), typeof(MathView), false, propertyChanged: OnInlineChanged);

    public static IMathFormulaRenderer? DefaultRenderer { get; set; }

    public string Latex
    {
        get => (string)GetValue(LatexProperty);
        set => SetValue(LatexProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public bool Inline
    {
        get => (bool)GetValue(InlineProperty);
        set => SetValue(InlineProperty, value);
    }

    public MathView()
    {
        InitializeComponent();
        Application.Current!.RequestedThemeChanged += OnThemeChanged;
        UpdateHint();
        UpdateLayout();
        RenderFormula();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        RenderFormula();
    }

    private static void OnRenderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((MathView)bindable).RenderFormula();
    }

    private static void OnHintChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((MathView)bindable).UpdateHint();
    }

    private static void OnInlineChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((MathView)bindable).UpdateLayout();
    }

    private void UpdateHint()
    {
        if (HintLabel == null)
        {
            return;
        }

        var hint = Hint ?? string.Empty;
        HintLabel.Text = hint;
        HintLabel.IsVisible = !string.IsNullOrWhiteSpace(hint);
    }

    private void UpdateLayout()
    {
        if (RootLayout == null)
        {
            return;
        }

        RootLayout.Orientation = Inline ? StackOrientation.Horizontal : StackOrientation.Vertical;
    }

    private void RenderFormula()
    {
        if (FormulaImage == null)
        {
            return;
        }

        var latex = Latex ?? string.Empty;
        if (string.IsNullOrWhiteSpace(latex))
        {
            FormulaImage.Source = null;
            return;
        }

        var renderer = DefaultRenderer ?? new CSharpMathFormulaRenderer();
        var theme = (Application.Current?.RequestedTheme ?? AppTheme.Unspecified) == AppTheme.Dark
            ? MathTheme.Dark
            : MathTheme.Light;
        var scale = (float)Math.Max(1, DeviceDisplay.MainDisplayInfo.Density);

        var result = renderer.Render(latex, (float)FontSize, theme, scale);
        if (result.PngBytes.Length == 0)
        {
            FormulaImage.Source = null;
            return;
        }

        FormulaImage.Source = ImageSource.FromStream(() => new MemoryStream(result.PngBytes));
    }
}
