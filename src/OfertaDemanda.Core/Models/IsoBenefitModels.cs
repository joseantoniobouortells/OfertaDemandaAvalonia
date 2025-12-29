using System;
using System.Collections.Generic;
using System.Linq;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Core.Numerics;

namespace OfertaDemanda.Core.Models;

public enum IsoProfitStatus
{
    Negative,
    Zero,
    Positive
}

public sealed record IsoBenefitFirmParameters(string Name, ParsedExpression TotalCost);

public sealed record IsoBenefitParameters(
    ParsedExpression DemandInverse,
    double DemandShock,
    IReadOnlyList<IsoBenefitFirmParameters> Firms,
    IReadOnlyList<double> FirmProfitLevels,
    IReadOnlyList<double> MarketProfitLevels);

public sealed record IsoProfitCurve(double TargetProfit, IReadOnlyList<ChartPoint> Points, ChartPoint? Intersection);

public sealed record IsoFirmResult(
    string Name,
    IReadOnlyList<IsoProfitCurve> Curves,
    ChartPoint? OptimalPoint,
    double OptimalQuantity,
    double OptimalProfit,
    IsoProfitStatus Status);

public sealed record IsoMarketResult(
    IReadOnlyList<ChartPoint> Demand,
    IReadOnlyList<IsoProfitCurve> Curves,
    ChartPoint? ReferencePoint,
    double ReferencePrice);

public sealed record IsoBenefitResult(
    double ReferencePrice,
    double ReferenceQuantity,
    IsoMarketResult Market,
    IReadOnlyList<IsoFirmResult> Firms,
    bool UsedFallbackPrice);

public static class IsoBenefitCalculator
{
    private const double FirmQuantityMin = 1;
    private const double FirmQuantityMax = 150;
    private const double FirmQuantityStep = 1;
    private const double MarketQuantityMin = 5;
    private const double MarketQuantityMax = 220;
    private const double MarketQuantityStep = 1;
    private const double PriceLow = 5;
    private const double PriceHigh = 150;
    private const double ProfitEpsilon = 1;
    private const double ShutdownTolerance = 1e-3;
    private const double DefaultReferencePrice = 40;

    public static IsoBenefitResult Calculate(IsoBenefitParameters parameters)
    {
        if (parameters.Firms.Count == 0)
        {
            throw new ArgumentException("Se requieren empresas para calcular isobeneficios.", nameof(parameters));
        }

        var contexts = parameters.Firms.Select(FirmContext.Create).ToArray();
        double Demand(double q) => NumericMethods.Safe(parameters.DemandInverse.Evaluate(q) + parameters.DemandShock);

        var referencePrice = ComputeReferencePrice(Demand, contexts, out var referenceQuantity, out var usedFallback);
        var marketResult = BuildMarketResult(parameters, contexts, Demand, referencePrice, referenceQuantity);
        var firmResults = BuildFirmResults(parameters, contexts, referencePrice);

        return new IsoBenefitResult(referencePrice, referenceQuantity, marketResult, firmResults, usedFallback);
    }

    private static double ComputeReferencePrice(Func<double, double> demand, IReadOnlyList<FirmContext> contexts, out double referenceQuantity, out bool usedFallback)
    {
        double Function(double price)
        {
            var quantity = TotalSupply(price, contexts);
            var demandValue = demand(Math.Max(quantity, 0));
            return demandValue - price;
        }

        var root = NumericMethods.FindRoot(Function, PriceLow, PriceHigh);
        usedFallback = double.IsNaN(root);
        var price = double.IsNaN(root) ? DefaultReferencePrice : root;
        referenceQuantity = TotalSupply(price, contexts);
        return price;
    }

    private static double TotalSupply(double price, IReadOnlyList<FirmContext> contexts)
    {
        var sum = 0d;
        foreach (var firm in contexts)
        {
            sum += SolveQuantityAtPrice(firm, price);
        }

        return NumericMethods.Safe(sum);
    }

    private static IsoMarketResult BuildMarketResult(
        IsoBenefitParameters parameters,
        IReadOnlyList<FirmContext> contexts,
        Func<double, double> demand,
        double referencePrice,
        double referenceQuantity)
    {
        var demandSamples = SampleRange(0, MarketQuantityMax, MarketQuantityStep)
            .Select(q => new ChartPoint(q, demand(q)))
            .ToArray();

        var marketCostFunc = BuildMarketCostFunction(contexts);
        var curves = parameters.MarketProfitLevels.Select(level =>
        {
            var samples = SampleRange(MarketQuantityMin, MarketQuantityMax, MarketQuantityStep)
                .Select(q =>
                {
                    var price = IsoMarketPrice(marketCostFunc, q, level);
                    return new ChartPoint(q, price);
                })
                .ToArray();

            var intersection = FindMarketIntersection(demand, marketCostFunc, level);
            return new IsoProfitCurve(level, samples, intersection);
        }).ToArray();

        ChartPoint? referencePoint = referenceQuantity > 0
            ? new ChartPoint(referenceQuantity, referencePrice)
            : null;

        return new IsoMarketResult(demandSamples, curves, referencePoint, referencePrice);
    }

    private static IsoProfitCurve FindFirmCurve(FirmContext context, double targetProfit, double referencePrice)
    {
        var samples = SampleRange(FirmQuantityMin, FirmQuantityMax, FirmQuantityStep)
            .Select(q =>
            {
                var price = IsoFirmPrice(context, q, targetProfit);
                return new ChartPoint(q, price);
            })
            .ToArray();

        var intersection = FindFirmIntersection(context, targetProfit, referencePrice);
        return new IsoProfitCurve(targetProfit, samples, intersection);
    }

    private static IReadOnlyList<IsoFirmResult> BuildFirmResults(IsoBenefitParameters parameters, IReadOnlyList<FirmContext> contexts, double referencePrice)
    {
        var results = new List<IsoFirmResult>(contexts.Count);
        foreach (var context in contexts)
        {
            var curves = parameters.FirmProfitLevels.Select(level => FindFirmCurve(context, level, referencePrice)).ToArray();
            var optimalQuantity = SolveQuantityAtPrice(context, referencePrice);
            ChartPoint? optimalPoint = null;
            double profit = 0;
            if (optimalQuantity > 0)
            {
                profit = NumericMethods.Safe(referencePrice * optimalQuantity - context.Cost(optimalQuantity));
                optimalPoint = new ChartPoint(optimalQuantity, referencePrice);
            }
            else
            {
                profit = -context.FixedCost;
            }

            var status = ResolveStatus(profit);
            results.Add(new IsoFirmResult(context.Name, curves, optimalPoint, optimalQuantity, profit, status));
        }

        return results;
    }

    private static IsoProfitStatus ResolveStatus(double profit)
    {
        if (profit > ProfitEpsilon)
        {
            return IsoProfitStatus.Positive;
        }

        if (profit < -ProfitEpsilon)
        {
            return IsoProfitStatus.Negative;
        }

        return IsoProfitStatus.Zero;
    }

    private static ChartPoint? FindFirmIntersection(FirmContext context, double profitLevel, double referencePrice)
    {
        var root = NumericMethods.FindRoot(q => IsoFirmPrice(context, q, profitLevel) - referencePrice, FirmQuantityMin, FirmQuantityMax);
        if (double.IsNaN(root) || root <= 0)
        {
            return null;
        }

        return new ChartPoint(root, referencePrice);
    }

    private static ChartPoint? FindMarketIntersection(Func<double, double> demand, Func<double, double> marketCost, double profitLevel)
    {
        var root = NumericMethods.FindRoot(q => IsoMarketPrice(marketCost, q, profitLevel) - demand(q), MarketQuantityMin, MarketQuantityMax);
        if (double.IsNaN(root) || root <= 0)
        {
            return null;
        }

        return new ChartPoint(root, demand(root));
    }

    private static Func<double, double> BuildMarketCostFunction(IReadOnlyList<FirmContext> contexts)
    {
        var count = contexts.Count;
        return Q =>
        {
            if (Q <= 0 || count == 0)
            {
                return 0;
            }

            var perFirm = Q / count;
            var sum = 0d;
            foreach (var firm in contexts)
            {
                sum += firm.Cost(perFirm);
            }

            return NumericMethods.Safe(sum);
        };
    }

    private static double IsoFirmPrice(FirmContext context, double quantity, double profitLevel)
    {
        if (quantity <= 0.001)
        {
            quantity = 0.001;
        }

        var cost = context.Cost(quantity);
        var price = (cost + profitLevel) / quantity;
        return NumericMethods.Safe(price);
    }

    private static double IsoMarketPrice(Func<double, double> marketCost, double quantity, double profitLevel)
    {
        if (quantity <= 0.001)
        {
            quantity = 0.001;
        }

        var cost = marketCost(quantity);
        var price = (cost + profitLevel) / quantity;
        return NumericMethods.Safe(price);
    }

    private static IEnumerable<double> SampleRange(double start, double end, double step)
    {
        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        for (var value = start; value <= end; value += step)
        {
            yield return value;
        }
    }

    private static double SolveQuantityAtPrice(FirmContext context, double price)
    {
        if (price <= 0)
        {
            return 0;
        }

        double Function(double q) => context.MarginalCost(q) - price;
        var root = NumericMethods.FindRoot(Function, 0, FirmQuantityMax);
        if (double.IsNaN(root) || root <= 0)
        {
            return 0;
        }

        var avc = context.AverageVariableCost(root);
        if (!double.IsNaN(avc) && price < avc - ShutdownTolerance)
        {
            return 0;
        }

        return NumericMethods.Safe(root);
    }

    private sealed class FirmContext
    {
        public FirmContext(string name, Func<double, double> cost)
        {
            Name = name;
            Cost = cost;
            FixedCost = Cost(0);
        }

        public string Name { get; }
        public Func<double, double> Cost { get; }
        public double FixedCost { get; }

        public double MarginalCost(double q) => NumericMethods.Derivative(Cost, q);

        public double AverageVariableCost(double q)
        {
            if (q <= 0.001)
            {
                return double.PositiveInfinity;
            }

            var total = Cost(q);
            return NumericMethods.Safe((total - FixedCost) / q);
        }

        public static FirmContext Create(IsoBenefitFirmParameters parameters)
        {
            double Cost(double q) => NumericMethods.Safe(parameters.TotalCost.Evaluate(q));
            var name = string.IsNullOrWhiteSpace(parameters.Name) ? "Empresa" : parameters.Name;
            return new FirmContext(name, Cost);
        }
    }
}
