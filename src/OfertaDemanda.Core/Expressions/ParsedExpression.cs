using System;
using System.Collections.Generic;

namespace OfertaDemanda.Core.Expressions;

/// <summary>
/// Represents a parsed mathematical expression ready to be evaluated with a q value.
/// </summary>
public sealed class ParsedExpression
{
    private readonly IReadOnlyList<RpnToken> _tokens;

    internal ParsedExpression(IReadOnlyList<RpnToken> tokens)
    {
        _tokens = tokens;
    }

    public double Evaluate(double q)
    {
        var stack = new Stack<double>();
        foreach (var token in _tokens)
        {
            switch (token.Type)
            {
                case RpnTokenType.Number:
                    stack.Push(token.Value);
                    break;
                case RpnTokenType.Variable:
                    stack.Push(q);
                    break;
                case RpnTokenType.Operator:
                    if (stack.Count < 2)
                    {
                        throw new InvalidOperationException("Invalid expression state.");
                    }

                    var right = stack.Pop();
                    var left = stack.Pop();
                    stack.Push(ApplyOperator(token.Operator, left, right));
                    break;
            }
        }

        if (stack.Count != 1)
        {
            throw new InvalidOperationException("Expression did not reduce to a single value.");
        }

        return stack.Pop();
    }

    private static double ApplyOperator(char op, double left, double right) =>
        op switch
        {
            '+' => left + right,
            '-' => left - right,
            '*' => left * right,
            '/' => right == 0 ? double.NaN : left / right,
            '^' => Math.Pow(left, right),
            _ => throw new InvalidOperationException($"Unsupported operator {op}")
        };

    internal readonly record struct RpnToken(RpnTokenType Type, double Value, char Operator);

    internal enum RpnTokenType
    {
        Number,
        Variable,
        Operator
    }
}
