using System;
using System.Collections.Generic;
using System.Globalization;
using OfertaDemanda.Core.Models;

namespace OfertaDemanda.Shared.Math;

public static class TotalCostFormulaBuilder
{
    private const string FormulaPrefix = "CT(q)=";

    public static string Build(
        MarketCostFunctionType type,
        double fixedCost,
        double linearCost,
        double quadraticCost,
        double cubicCost,
        CultureInfo culture)
    {
        var variableTerms = new List<Term>();

        AddVariableTerm(variableTerms, linearCost, "q", culture);
        AddVariableTerm(variableTerms, quadraticCost, "q^2", culture);

        if (type == MarketCostFunctionType.Cubic)
        {
            AddVariableTerm(variableTerms, cubicCost, "q^3", culture);
        }

        var terms = new List<Term>();
        if (fixedCost != 0 || variableTerms.Count == 0)
        {
            AddConstantTerm(terms, fixedCost, culture);
        }

        terms.AddRange(variableTerms);

        var expression = string.Join(" ", BuildExpressionParts(terms));
        return $"{FormulaPrefix}{expression}";
    }

    private static IEnumerable<string> BuildExpressionParts(IReadOnlyList<Term> terms)
    {
        for (var i = 0; i < terms.Count; i++)
        {
            var term = terms[i];
            if (i == 0)
            {
                yield return term.Sign == "-" ? $"{term.Sign}{term.Text}" : term.Text;
            }
            else
            {
                yield return $"{term.Sign} {term.Text}";
            }
        }
    }

    private static void AddVariableTerm(List<Term> terms, double coefficient, string variable, CultureInfo culture)
    {
        if (coefficient == 0)
        {
            return;
        }

        var sign = coefficient < 0 ? "-" : "+";
        var abs = System.Math.Abs(coefficient);
        var coefficientText = abs == 1 ? string.Empty : FormatNumber(abs, culture);
        terms.Add(new Term(sign, $"{coefficientText}{variable}"));
    }

    private static void AddConstantTerm(List<Term> terms, double value, CultureInfo culture)
    {
        if (value == 0 && terms.Count > 0)
        {
            return;
        }

        var sign = value < 0 ? "-" : "+";
        var abs = System.Math.Abs(value);
        var text = FormatNumber(abs, culture);
        terms.Add(new Term(sign, text));
    }

    private static string FormatNumber(double value, CultureInfo culture)
    {
        return value.ToString("0.###", culture);
    }

    private readonly record struct Term(string Sign, string Text);
}
