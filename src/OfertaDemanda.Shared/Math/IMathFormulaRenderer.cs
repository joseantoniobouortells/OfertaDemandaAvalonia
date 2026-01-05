using System;

namespace OfertaDemanda.Shared.Math;

public enum MathTheme
{
    Light,
    Dark
}

public sealed record MathRenderResult(byte[] PngBytes, int Width, int Height);

public interface IMathFormulaRenderer
{
    MathRenderResult Render(string latex, float fontSize, MathTheme theme, float dpiScale);
    void ClearCache();
}
