using System;
using System.Collections.Generic;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Core.Numerics;

namespace OfertaDemanda.Core.Models;

public sealed record MonopolyParameters(
    ParsedExpression DemandInverse,
    ParsedExpression TotalCost);

public sealed record MonopolyResult(
    IReadOnlyList<ChartPoint> Demand,
    IReadOnlyList<ChartPoint> MarginalRevenue,
    IReadOnlyList<ChartPoint> MarginalCost,
    ChartPoint? MonopolyPoint,
    ChartPoint? CompetitivePoint,
    double? Profit,
    double? DeadweightLoss,
    IReadOnlyList<string> Errors);

public static class MonopolyCalculator
{
    private const int SampleCount = 100;

    public static MonopolyResult Calculate(MonopolyParameters parameters)
    {
        var errors = new List<string>();
        double Demand(double q) => NumericMethods.Safe(parameters.DemandInverse.Evaluate(q));
        double Cost(double q) => NumericMethods.Safe(parameters.TotalCost.Evaluate(q));
        double Revenue(double q) => NumericMethods.Safe(Demand(q) * q);
        double MarginalRevenue(double q) => NumericMethods.Derivative(Revenue, q);
        double MarginalCost(double q) => NumericMethods.Derivative(Cost, q);

        var demandPoints = BuildPoints(Demand);
        var mrPoints = BuildPoints(MarginalRevenue);
        var mcPoints = BuildPoints(MarginalCost);

        ChartPoint? monopoly = null;
        double? profit = null;
        var qm = NumericMethods.FindRoot(q => MarginalRevenue(q) - MarginalCost(q), 0, 300);
        if (double.IsNaN(qm))
        {
            errors.Add("No se encontró q_m tal que IMg = CMg.");
        }
        else
        {
            var pm = Demand(qm);
            monopoly = new ChartPoint(qm, pm);
            profit = NumericMethods.Safe(Revenue(qm) - Cost(qm));
        }

        ChartPoint? competitive = null;
        var qcp = NumericMethods.FindRoot(q => Demand(q) - MarginalCost(q), 0, 300);
        if (double.IsNaN(qcp))
        {
            errors.Add("No se encontró el equilibrio de referencia CP.");
        }
        else
        {
            competitive = new ChartPoint(qcp, Demand(qcp));
        }

        double? dwl = null;
        if (monopoly.HasValue && competitive.HasValue)
        {
            var qmVal = monopoly.Value.X;
            var qcpVal = competitive.Value.X;
            if (Math.Abs(qmVal - qcpVal) > 1e-3)
            {
                var integral = NumericMethods.Integrate(q => Math.Max(0, Demand(q) - MarginalCost(q)), qmVal, qcpVal);
                dwl = Math.Abs(integral);
            }
            else
            {
                dwl = 0;
            }
        }

        return new MonopolyResult(
            demandPoints,
            mrPoints,
            mcPoints,
            monopoly,
            competitive,
            profit,
            dwl,
            errors);
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
