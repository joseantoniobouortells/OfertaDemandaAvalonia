using System;

namespace OfertaDemanda.Core.Numerics;

public static class NumericMethods
{
    private const double ClampLimit = 1_000_000d;

    public static double Safe(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        if (double.IsPositiveInfinity(value))
        {
            return ClampLimit;
        }

        if (double.IsNegativeInfinity(value))
        {
            return -ClampLimit;
        }

        if (value > ClampLimit)
        {
            return ClampLimit;
        }

        if (value < -ClampLimit)
        {
            return -ClampLimit;
        }

        return value;
    }

    public static double EvaluateSafe(Func<double, double> f, double x)
    {
        try
        {
            return Safe(f(x));
        }
        catch
        {
            return double.NaN;
        }
    }

    public static double Derivative(Func<double, double> f, double x, double h = 1e-4)
    {
        var forward = EvaluateSafe(f, x + h);
        var backward = EvaluateSafe(f, x - h);
        if (double.IsNaN(forward) || double.IsNaN(backward))
        {
            return double.NaN;
        }

        return Safe((forward - backward) / (2 * h));
    }

    public static double Integrate(Func<double, double> f, double start, double end, int steps = 400)
    {
        if (steps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steps));
        }

        var range = end - start;
        if (Math.Abs(range) < 1e-6)
        {
            return 0;
        }

        var step = range / steps;
        var sum = 0d;
        var previous = EvaluateSafe(f, start);
        for (var i = 1; i <= steps; i++)
        {
            var x = start + i * step;
            var current = EvaluateSafe(f, x);
            if (double.IsNaN(previous) || double.IsNaN(current))
            {
                continue;
            }

            sum += (previous + current) * 0.5 * step;
            previous = current;
        }

        return Safe(sum);
    }

    public static double FindRoot(Func<double, double> f, double low = 0, double high = 1000, double tolerance = 1e-4, int maxIterations = 100)
    {
        var a = low;
        var b = high;
        var fa = EvaluateSafe(f, a);
        var fb = EvaluateSafe(f, b);

        double sampled = double.NaN;
        if (double.IsNaN(fa) || double.IsNaN(fb) || Math.Sign(fa) == Math.Sign(fb))
        {
            sampled = SampleForRoot(f, low, high, 400, out var bracketLow, out var bracketHigh);
            if (!double.IsNaN(bracketLow))
            {
                a = bracketLow;
                b = bracketHigh;
                fa = EvaluateSafe(f, a);
                fb = EvaluateSafe(f, b);
            }
            else
            {
                return sampled;
            }
        }

        if (double.IsNaN(fa) || double.IsNaN(fb))
        {
            return double.IsNaN(sampled) ? double.NaN : Safe(sampled);
        }

        var mid = 0d;
        for (var i = 0; i < maxIterations; i++)
        {
            mid = 0.5 * (a + b);
            var fm = EvaluateSafe(f, mid);
            if (double.IsNaN(fm))
            {
                break;
            }

            if (Math.Abs(fm) < tolerance)
            {
                return Safe(mid);
            }

            if (Math.Sign(fa) == Math.Sign(fm))
            {
                a = mid;
                fa = fm;
            }
            else
            {
                b = mid;
                fb = fm;
            }
        }

        return Safe(mid);
    }

    private static double SampleForRoot(Func<double, double> f, double low, double high, int samples, out double bracketLow, out double bracketHigh)
    {
        bracketLow = double.NaN;
        bracketHigh = double.NaN;

        var bestValue = double.PositiveInfinity;
        var bestX = double.NaN;
        var prevX = low;
        var prevValue = EvaluateSafe(f, prevX);

        for (var i = 1; i <= samples; i++)
        {
            var t = (double)i / samples;
            var x = low + (high - low) * t;
            var value = EvaluateSafe(f, x);
            if (!double.IsNaN(value) && Math.Abs(value) < bestValue)
            {
                bestValue = Math.Abs(value);
                bestX = x;
            }

            if (!double.IsNaN(value) && !double.IsNaN(prevValue) && Math.Sign(value) != Math.Sign(prevValue))
            {
                bracketLow = prevX;
                bracketHigh = x;
                return bestX;
            }

            prevX = x;
            prevValue = value;
        }

        return bestX;
    }
}
