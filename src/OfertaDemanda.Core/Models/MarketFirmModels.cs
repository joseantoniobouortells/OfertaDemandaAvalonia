using System;
using System.Collections.Generic;
using System.Linq;
using OfertaDemanda.Core.Numerics;

namespace OfertaDemanda.Core.Models;

public enum MarketCostFunctionType
{
    Quadratic,
    Cubic
}

public sealed record MarketCostParameters(
    MarketCostFunctionType Type,
    double FixedCost,
    double LinearCost,
    double QuadraticCost,
    double CubicCost);

public sealed record MarketFirmResult(
    IReadOnlyList<ChartPoint> MarginalCost,
    IReadOnlyList<ChartPoint> AverageCost,
    IReadOnlyList<ChartPoint> AverageVariableCost,
    double? OptimalQuantity,
    double? MarginalCostAtOptimal,
    double? AverageCostAtOptimal,
    double? AverageVariableCostAtOptimal,
    double? ProfitAtOptimal,
    double? ShutdownQuantity,
    double? ShutdownPrice,
    IReadOnlyList<double> BreakEvenQuantities,
    IReadOnlyList<string> Errors);

public static class MarketFirmCalculator
{
    private const int SampleCount = 120;
    private const double MinimumQuantity = 0.05;

    public static MarketFirmResult Calculate(MarketCostParameters parameters, double price, double maxQuantity = 100d)
    {
        var errors = new List<string>();
        var cubic = parameters.Type == MarketCostFunctionType.Cubic ? parameters.CubicCost : 0d;

        double Cost(double q) => NumericMethods.Safe(parameters.FixedCost
            + parameters.LinearCost * q
            + parameters.QuadraticCost * q * q
            + cubic * q * q * q);

        double MarginalCost(double q) => NumericMethods.Safe(parameters.LinearCost
            + 2d * parameters.QuadraticCost * q
            + 3d * cubic * q * q);

        double AverageCost(double q)
        {
            var safeQ = q < MinimumQuantity ? MinimumQuantity : q;
            return NumericMethods.Safe(Cost(safeQ) / safeQ);
        }

        double AverageVariableCost(double q)
        {
            var safeQ = q < MinimumQuantity ? MinimumQuantity : q;
            return NumericMethods.Safe((Cost(safeQ) - parameters.FixedCost) / safeQ);
        }

        var mcPoints = BuildPoints(MarginalCost, maxQuantity);
        var acPoints = BuildPoints(AverageCost, maxQuantity);
        var avcPoints = BuildPoints(AverageVariableCost, maxQuantity);

        var (shutdownQuantity, shutdownPrice) = FindShutdownPoint(parameters, AverageVariableCost, maxQuantity);

        double? optimalQuantity = null;
        if (!shutdownPrice.HasValue || price < shutdownPrice.Value)
        {
            optimalQuantity = 0;
        }
        else
        {
            var roots = FindRoots(q => MarginalCost(q) - price, 0, maxQuantity);
            if (roots.Count == 0)
            {
                errors.Add("No se encontró q* para CMg = P.");
            }
            else
            {
                optimalQuantity = roots
                    .Where(q => q >= 0)
                    .OrderByDescending(q => price * q - Cost(q))
                    .First();
            }
        }

        double? marginalAtOptimal = null;
        double? averageAtOptimal = null;
        double? averageVariableAtOptimal = null;
        double? profit = null;
        if (optimalQuantity.HasValue)
        {
            var q = optimalQuantity.Value;
            marginalAtOptimal = MarginalCost(q);
            averageAtOptimal = AverageCost(q);
            averageVariableAtOptimal = AverageVariableCost(q);
            profit = NumericMethods.Safe(price * q - Cost(q));
        }

        var breakEven = FindRoots(q => Cost(q) - price * q, MinimumQuantity, maxQuantity);

        return new MarketFirmResult(
            mcPoints,
            acPoints,
            avcPoints,
            optimalQuantity,
            marginalAtOptimal,
            averageAtOptimal,
            averageVariableAtOptimal,
            profit,
            shutdownQuantity,
            shutdownPrice,
            breakEven,
            errors);
    }

    private static IReadOnlyList<ChartPoint> BuildPoints(Func<double, double> f, double maxQuantity)
    {
        var points = new ChartPoint[SampleCount];
        var step = maxQuantity / (SampleCount - 1);
        for (var i = 0; i < SampleCount; i++)
        {
            var q = i * step;
            points[i] = new ChartPoint(q, NumericMethods.Safe(f(q)));
        }

        return points;
    }

    private static (double? quantity, double? price) FindShutdownPoint(
        MarketCostParameters parameters,
        Func<double, double> averageVariableCost,
        double maxQuantity)
    {
        if (parameters.Type == MarketCostFunctionType.Quadratic && parameters.QuadraticCost >= 0)
        {
            return (MinimumQuantity, NumericMethods.Safe(parameters.LinearCost));
        }

        if (parameters.Type == MarketCostFunctionType.Cubic && parameters.CubicCost > 0)
        {
            var q = -parameters.QuadraticCost / (2d * parameters.CubicCost);
            if (double.IsNaN(q) || q < MinimumQuantity)
            {
                q = MinimumQuantity;
            }

            q = Math.Min(q, maxQuantity);
            return (q, NumericMethods.Safe(averageVariableCost(q)));
        }

        var minValue = double.PositiveInfinity;
        var minQ = MinimumQuantity;
        var samples = 200;
        var step = maxQuantity / samples;
        for (var i = 1; i <= samples; i++)
        {
            var q = i * step;
            var value = averageVariableCost(q);
            if (double.IsNaN(value))
            {
                continue;
            }

            if (value < minValue)
            {
                minValue = value;
                minQ = q;
            }
        }

        return double.IsInfinity(minValue)
            ? (null, null)
            : (minQ, NumericMethods.Safe(minValue));
    }

    private static List<double> FindRoots(Func<double, double> f, double low, double high, int samples = 400)
    {
        var roots = new List<double>();
        var prevX = low;
        var prevValue = NumericMethods.EvaluateSafe(f, prevX);
        var tolerance = 1e-3;
        if (!double.IsNaN(prevValue) && Math.Abs(prevValue) < tolerance)
        {
            roots.Add(prevX);
        }

        for (var i = 1; i <= samples; i++)
        {
            var t = (double)i / samples;
            var x = low + (high - low) * t;
            var value = NumericMethods.EvaluateSafe(f, x);

            if (!double.IsNaN(value) && !double.IsNaN(prevValue) && Math.Sign(value) != Math.Sign(prevValue))
            {
                var root = NumericMethods.FindRoot(f, prevX, x);
                if (!double.IsNaN(root))
                {
                    roots.Add(root);
                }
            }

            prevX = x;
            prevValue = value;
        }

        return roots
            .Where(q => !double.IsNaN(q))
            .OrderBy(q => q)
            .Aggregate(new List<double>(), (acc, q) =>
            {
                if (acc.Count == 0 || Math.Abs(acc[^1] - q) > 1e-2)
                {
                    acc.Add(NumericMethods.Safe(q));
                }

                return acc;
            });
    }
}
