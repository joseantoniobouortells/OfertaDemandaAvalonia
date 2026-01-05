using System.Globalization;
using OfertaDemanda.Core.Models;
using OfertaDemanda.Shared.Math;

namespace OfertaDemanda.Core.Tests;

public class TotalCostFormulaBuilderTests
{
    [Theory]
    [InlineData("es-ES", "CT(q)=50 + 8q + 0,4q^2")]
    [InlineData("en-US", "CT(q)=50 + 8q + 0.4q^2")]
    public void QuadraticFormatsUsingCulture(string cultureName, string expected)
    {
        var culture = new CultureInfo(cultureName);

        var formula = TotalCostFormulaBuilder.Build(
            MarketCostFunctionType.Quadratic,
            fixedCost: 50,
            linearCost: 8,
            quadraticCost: 0.4,
            cubicCost: 0,
            culture);

        Assert.Equal(expected, formula);
    }

    [Fact]
    public void NegativeLinearUsesMinusSign()
    {
        var formula = TotalCostFormulaBuilder.Build(
            MarketCostFunctionType.Quadratic,
            fixedCost: 0,
            linearCost: -2,
            quadraticCost: 1,
            cubicCost: 0,
            CultureInfo.InvariantCulture);

        Assert.Equal("CT(q)=-2q + q^2", formula);
    }

    [Fact]
    public void OmitsZeroTermsAndKeepsFixedCost()
    {
        var formula = TotalCostFormulaBuilder.Build(
            MarketCostFunctionType.Quadratic,
            fixedCost: 50,
            linearCost: 0,
            quadraticCost: 0,
            cubicCost: 0,
            CultureInfo.InvariantCulture);

        Assert.Equal("CT(q)=50", formula);
    }

    [Fact]
    public void ReturnsZeroWhenAllTermsZero()
    {
        var formula = TotalCostFormulaBuilder.Build(
            MarketCostFunctionType.Quadratic,
            fixedCost: 0,
            linearCost: 0,
            quadraticCost: 0,
            cubicCost: 0,
            CultureInfo.InvariantCulture);

        Assert.Equal("CT(q)=0", formula);
    }

    [Fact]
    public void CubicIncludesThirdOrderTerm()
    {
        var formula = TotalCostFormulaBuilder.Build(
            MarketCostFunctionType.Cubic,
            fixedCost: 10,
            linearCost: 0,
            quadraticCost: 0,
            cubicCost: -1,
            CultureInfo.InvariantCulture);

        Assert.Equal("CT(q)=10 - q^3", formula);
    }
}
