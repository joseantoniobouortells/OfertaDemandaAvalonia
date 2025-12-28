using System;
using System.Collections.Generic;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Core.Numerics;

namespace OfertaDemanda.Core.Models;

public sealed record ElasticityParameters(
    ParsedExpression DemandInverse,
    double DemandShock,
    double Price);

public sealed record ElasticityResult(
    IReadOnlyList<ChartPoint> Demand,
    ChartPoint? Point,
    double? Elasticity,
    IReadOnlyList<string> Errors);

public static class ElasticityCalculator
{
    private const int SampleCount = 150;

    public static ElasticityResult Calculate(ElasticityParameters parameters)
    {
        var errors = new List<string>();
        double Demand(double q) => NumericMethods.Safe(parameters.DemandInverse.Evaluate(q) + parameters.DemandShock);
        var points = BuildPoints(Demand);

        ChartPoint? marker = null;
        double? elasticity = null;

        var qAtPrice = NumericMethods.FindRoot(q => Demand(q) - parameters.Price, 0, 500);
        if (double.IsNaN(qAtPrice) || qAtPrice <= 0)
        {
            errors.Add("No se encontró la cantidad asociada al precio seleccionado.");
        }
        else
        {
            var dpdq = NumericMethods.Derivative(Demand, qAtPrice);
            if (double.IsNaN(dpdq) || Math.Abs(dpdq) < 1e-6)
            {
                errors.Add("Elasticidad no computable (derivada cercana a 0).");
                marker = new ChartPoint(qAtPrice, parameters.Price);
            }
            else
            {
                elasticity = NumericMethods.Safe(System.Math.Abs((1 / dpdq) * (parameters.Price / qAtPrice)));
                marker = new ChartPoint(qAtPrice, parameters.Price);
            }
        }

        return new ElasticityResult(points, marker, elasticity, errors);
    }

    private static IReadOnlyList<ChartPoint> BuildPoints(Func<double, double> f)
    {
        var points = new ChartPoint[SampleCount];
        for (var i = 0; i < SampleCount; i++)
        {
            var q = i;
            points[i] = new ChartPoint(q, NumericMethods.Safe(f(q)));
        }

        return points;
    }
}
