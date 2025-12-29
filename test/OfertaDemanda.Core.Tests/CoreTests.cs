using System;
using OfertaDemanda.Core.Expressions;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Core.Numerics;

namespace OfertaDemanda.Core.Tests;

public class ExpressionTests
{
    [Theory]
    [InlineData("100 - 0.5q", 20, 90)]
    [InlineData("20 + 0.5q", 40, 40)]
    [InlineData("200 + 10q + 0.5q^2", 10, 350)]
    [InlineData("120 - q", 15, 105)]
    [InlineData("100 + 10q + 0.2q^2", 5, 155)]
    public void ParsesDefaultExpressions(string expression, double q, double expected)
    {
        var parsed = ExpressionParser.Parse(expression);
        var value = parsed.Evaluate(q);
        Assert.Equal(expected, value, 3);
    }

    [Fact]
    public void SupportsImplicitMultiplication()
    {
        var parsed = ExpressionParser.Parse("(q+1)(q-1)");
        var value = parsed.Evaluate(3);
        Assert.Equal(8, value, 3);
    }
}

public class NumericMethodsTests
{
    [Fact]
    public void FindsSimpleRoot()
    {
        var root = NumericMethods.FindRoot(q => q - 10, 0, 20);
        Assert.InRange(root, 9.9, 10.1);
    }

    [Fact]
    public void ApproximatesDerivative()
    {
        var derivative = NumericMethods.Derivative(q => q * q, 5);
        Assert.InRange(derivative, 9.9, 10.1);
    }

    [Fact]
    public void ApproximatesIntegral()
    {
        var integral = NumericMethods.Integrate(q => q, 0, 1);
        Assert.InRange(integral, 0.49, 0.51);
    }
}

public class ModelTests
{
    [Fact]
    public void MarketDefaultsMatchReferenceEquilibrium()
    {
        var demand = ExpressionParser.Parse("100 - 0.5q");
        var supply = ExpressionParser.Parse("20 + 0.5q");
        var result = MarketCalculator.Calculate(new MarketParameters(demand, supply, 0, 0, 0));

        Assert.NotNull(result.Equilibrium);
        var equilibrium = result.Equilibrium!.Value;
        Assert.InRange(equilibrium.X, 79.5, 80.5);
        Assert.InRange(equilibrium.Y, 59.5, 60.5);
        Assert.True(result.ConsumerSurplus.GetValueOrDefault() > 0);
        Assert.True(result.ProducerSurplus.GetValueOrDefault() > 0);
    }

    [Fact]
    public void MonopolyReferenceProducesPositiveProfit()
    {
        var demand = ExpressionParser.Parse("120 - q");
        var cost = ExpressionParser.Parse("100 + 10q + 0.2q^2");
        var result = MonopolyCalculator.Calculate(new MonopolyParameters(demand, cost));

        Assert.NotNull(result.MonopolyPoint);
        var monopoly = result.MonopolyPoint!.Value;
        Assert.InRange(monopoly.X, 45, 46);
        Assert.InRange(monopoly.Y, 73.5, 74.5);
        Assert.True(result.Profit.GetValueOrDefault() > 0);
        Assert.True(result.DeadweightLoss.GetValueOrDefault() > 0);
    }
}

public class IsoBenefitCalculatorTests
{
    [Fact]
    public void ComputesIsobenefitCurves()
    {
        var demand = ExpressionParser.Parse("100 - 0.5q");
        var firms = new[]
        {
            new IsoBenefitFirmParameters("A", ExpressionParser.Parse("200 + 10q + 0.5q^2")),
            new IsoBenefitFirmParameters("B", ExpressionParser.Parse("120 + 12q + 0.3q^2")),
            new IsoBenefitFirmParameters("C", ExpressionParser.Parse("80 + 8q + 0.8q^2"))
        };

        var parameters = new IsoBenefitParameters(
            demand,
            0,
            firms,
            new[] { -200d, 0d, 200d },
            new[] { -500d, 0d, 500d });

        var result = IsoBenefitCalculator.Calculate(parameters);

        Assert.True(result.ReferencePrice > 0);
        Assert.Equal(3, result.Firms.Count);
        Assert.Equal(3, result.Market.Curves.Count);
    }
}
