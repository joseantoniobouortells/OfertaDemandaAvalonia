using System;
using System.Collections.Generic;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Core.Numerics;

namespace OfertaDemanda.Core.Models;

public sealed record MarketParameters(
    ParsedExpression DemandInverse,
    ParsedExpression SupplyInverse,
    double DemandShock,
    double SupplyShock,
    double Tax);

public sealed record MarketResult(
    IReadOnlyList<ChartPoint> DemandBase,
    IReadOnlyList<ChartPoint> SupplyBase,
    IReadOnlyList<ChartPoint> DemandShifted,
    IReadOnlyList<ChartPoint> SupplyShifted,
    ChartPoint? Equilibrium,
    ChartPoint? NoTaxEquilibrium,
    double? ProducerPrice,
    double? ConsumerSurplus,
    double? ProducerSurplus,
    double? TaxRevenue,
    double? DeadweightLoss,
    IReadOnlyList<AreaSamplePoint> ConsumerArea,
    IReadOnlyList<AreaSamplePoint> ProducerArea,
    IReadOnlyList<AreaSamplePoint> DeadweightArea,
    IReadOnlyList<string> Errors);

public static class MarketCalculator
{
    private const int SampleCount = 100;
    private const double SampleStep = 2d;

    public static MarketResult Calculate(MarketParameters parameters)
    {
        var errors = new List<string>();
        double DemandBase(double q) => NumericMethods.Safe(parameters.DemandInverse.Evaluate(q));
        double SupplyBase(double q) => NumericMethods.Safe(parameters.SupplyInverse.Evaluate(q));
        double DemandShifted(double q) => NumericMethods.Safe(DemandBase(q) + parameters.DemandShock);
        double SupplyShifted(double q) => NumericMethods.Safe(SupplyBase(q) - parameters.SupplyShock);

        var demand0 = BuildPoints(DemandBase);
        var supply0 = BuildPoints(SupplyBase);
        var demand1 = BuildPoints(DemandShifted);
        var supply1 = BuildPoints(SupplyShifted);

        var eqQuantity = NumericMethods.FindRoot(q => DemandShifted(q) - (SupplyShifted(q) + parameters.Tax));
        ChartPoint? equilibrium = null;
        double? consumerSurplus = null;
        double? producerSurplus = null;
        double? taxRevenue = null;

        double? producerPrice = null;
        IReadOnlyList<AreaSamplePoint> consumerAreaSamples = Array.Empty<AreaSamplePoint>();
        IReadOnlyList<AreaSamplePoint> producerAreaSamples = Array.Empty<AreaSamplePoint>();
        IReadOnlyList<AreaSamplePoint> deadweightAreaSamples = Array.Empty<AreaSamplePoint>();

        if (double.IsNaN(eqQuantity))
        {
            errors.Add("No se encontró el equilibrio con impuesto.");
        }
        else
        {
            var pc = NumericMethods.Safe(DemandShifted(eqQuantity));
            var pp = NumericMethods.Safe(pc - parameters.Tax);
            equilibrium = new ChartPoint(eqQuantity, pc);
             producerPrice = pp;
            consumerSurplus = NumericMethods.Integrate(q => Math.Max(0, DemandShifted(q) - pc), 0, eqQuantity);
            producerSurplus = NumericMethods.Integrate(q => Math.Max(0, pp - SupplyShifted(q)), 0, eqQuantity);
            taxRevenue = NumericMethods.Safe(parameters.Tax * eqQuantity);
            consumerAreaSamples = BuildAreaSamples(0, eqQuantity, _ => pc, DemandShifted);
            producerAreaSamples = BuildAreaSamples(0, eqQuantity, SupplyShifted, _ => pp);
        }

        var qNoTax = NumericMethods.FindRoot(q => DemandShifted(q) - SupplyShifted(q));
        ChartPoint? noTaxEquilibrium = null;
        double? dwl = null;
        if (double.IsNaN(qNoTax))
        {
            errors.Add("No se encontró el equilibrio sin impuesto.");
        }
        else
        {
            noTaxEquilibrium = new ChartPoint(qNoTax, DemandShifted(qNoTax));
        }

        if (equilibrium.HasValue && noTaxEquilibrium.HasValue)
        {
            var from = equilibrium.Value.X;
            var to = noTaxEquilibrium.Value.X;
            if (Math.Abs(from - to) > 1e-3)
            {
                var integral = NumericMethods.Integrate(q => Math.Max(0, DemandShifted(q) - SupplyShifted(q)), from, to);
                dwl = Math.Abs(integral);
                deadweightAreaSamples = BuildAreaSamples(from, to, SupplyShifted, DemandShifted);
            }
            else
            {
                dwl = 0;
                deadweightAreaSamples = Array.Empty<AreaSamplePoint>();
            }
        }

        return new MarketResult(
            demand0,
            supply0,
            demand1,
            supply1,
            equilibrium,
            noTaxEquilibrium,
            producerPrice,
            consumerSurplus,
            producerSurplus,
            taxRevenue,
            dwl,
            consumerAreaSamples,
            producerAreaSamples,
            deadweightAreaSamples,
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

    private static IReadOnlyList<AreaSamplePoint> BuildAreaSamples(double start, double end, Func<double, double> baseFunc, Func<double, double> topFunc, int steps = 80)
    {
        if (double.IsNaN(start) || double.IsNaN(end) || Math.Abs(end - start) < 1e-6)
        {
            return Array.Empty<AreaSamplePoint>();
        }

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var samples = new AreaSamplePoint[steps];
        for (var i = 0; i < steps; i++)
        {
            var t = steps == 1 ? 0 : (double)i / (steps - 1);
            var q = start + (end - start) * t;
            var baseVal = NumericMethods.Safe(baseFunc(q));
            var topVal = NumericMethods.Safe(topFunc(q));
            var offset = Math.Max(0, topVal - baseVal);
            samples[i] = new AreaSamplePoint(q, baseVal, offset);
        }

        return samples;
    }
}

public readonly record struct AreaSamplePoint(double X, double BaseValue, double OffsetValue);
