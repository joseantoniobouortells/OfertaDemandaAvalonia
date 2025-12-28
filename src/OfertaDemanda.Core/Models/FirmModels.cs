using System;
using System.Collections.Generic;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Core.Numerics;

namespace OfertaDemanda.Core.Models;

public enum FirmMode
{
    ShortRun,
    LongRun
}

public sealed record FirmParameters(
    ParsedExpression TotalCost,
    double Price,
    FirmMode Mode);

public sealed record FirmResult(
    IReadOnlyList<ChartPoint> MarginalCost,
    IReadOnlyList<ChartPoint> AverageCost,
    IReadOnlyList<ChartPoint> AverageVariableCost,
    double PriceLine,
    ChartPoint? QuantityPoint,
    double? Profit,
    IReadOnlyList<string> Errors);

public static class FirmCalculator
{
    private const int SampleCount = 100;
    private const double SampleStep = 0.6;

    public static FirmResult Calculate(FirmParameters parameters)
    {
        var errors = new List<string>();
        double Cost(double q) => NumericMethods.Safe(parameters.TotalCost.Evaluate(q));
        var fixedCost = Cost(0);

        double MarginalCost(double q) => NumericMethods.Derivative(Cost, q);
        double AverageCost(double q)
        {
            var safeQ = q < 0.01 ? 0.01 : q;
            return NumericMethods.Safe(Cost(safeQ) / safeQ);
        }

        double AverageVariableCost(double q)
        {
            if (q <= 0.01)
            {
                return 0;
            }

            return NumericMethods.Safe((Cost(q) - fixedCost) / q);
        }

        var mcPoints = BuildPoints(MarginalCost);
        var acPoints = BuildPoints(AverageCost);
        var avcPoints = BuildPoints(AverageVariableCost);

        double priceLine;
        double? quantity;
        if (parameters.Mode == FirmMode.ShortRun)
        {
            priceLine = parameters.Price;
            var root = NumericMethods.FindRoot(q => MarginalCost(q) - priceLine, 0, 300);
            if (double.IsNaN(root))
            {
                errors.Add("No se encontró q* para CMg = P.");
            }
            quantity = double.IsNaN(root) ? null : root;
        }
        else
        {
            var root = NumericMethods.FindRoot(q => MarginalCost(q) - AverageCost(q), 0.1, 500);
            if (double.IsNaN(root))
            {
                errors.Add("No se encontró el mínimo de CMe.");
                quantity = null;
                priceLine = parameters.Price;
            }
            else
            {
                quantity = root;
                priceLine = AverageCost(root);
            }
        }

        ChartPoint? quantityPoint = null;
        double? profit = null;
        if (quantity.HasValue)
        {
            var q = quantity.Value;
            var benefit = NumericMethods.Safe(priceLine * q - Cost(q));
            profit = benefit;
            quantityPoint = new ChartPoint(q, priceLine);
        }

        return new FirmResult(
            mcPoints,
            acPoints,
            avcPoints,
            priceLine,
            quantityPoint,
            profit,
            errors);
    }

    private static IReadOnlyList<ChartPoint> BuildPoints(Func<double, double> f)
    {
        var points = new ChartPoint[SampleCount];
        for (var i = 0; i < SampleCount; i++)
        {
            var q = i * SampleStep;
            points[i] = new ChartPoint(q, NumericMethods.Safe(f(q)));
        }

        return points;
    }
}
