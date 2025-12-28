using System;
using System.Collections.Generic;
using System.Globalization;

namespace OfertaDemanda.Core.Expressions;

public static class ExpressionParser
{
    public static ParsedExpression Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ExpressionParseException("La expresión está vacía.", 0);
        }

        var tokens = Tokenize(raw);
        var rpn = ToReversePolish(tokens);
        return new ParsedExpression(rpn);
    }

    private static IReadOnlyList<ParsedExpression.RpnToken> ToReversePolish(IReadOnlyList<Token> tokens)
    {
        var output = new List<ParsedExpression.RpnToken>();
        var operators = new Stack<Token>();

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    output.Add(new ParsedExpression.RpnToken(ParsedExpression.RpnTokenType.Number, token.Number, '\0'));
                    break;
                case TokenType.Variable:
                    output.Add(new ParsedExpression.RpnToken(ParsedExpression.RpnTokenType.Variable, 0, '\0'));
                    break;
                case TokenType.Operator:
                    while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator &&
                           (HasHigherPrecedence(operators.Peek().Operator, token.Operator) ||
                           (HasEqualPrecedence(operators.Peek().Operator, token.Operator) && !IsRightAssociative(token.Operator))))
                    {
                        var op = operators.Pop();
                        output.Add(new ParsedExpression.RpnToken(ParsedExpression.RpnTokenType.Operator, 0, op.Operator));
                    }

                    operators.Push(token);
                    break;
                case TokenType.LeftParen:
                    operators.Push(token);
                    break;
                case TokenType.RightParen:
                    var found = false;
                    while (operators.Count > 0)
                    {
                        var op = operators.Pop();
                        if (op.Type == TokenType.LeftParen)
                        {
                            found = true;
                            break;
                        }

                        output.Add(new ParsedExpression.RpnToken(ParsedExpression.RpnTokenType.Operator, 0, op.Operator));
                    }

                    if (!found)
                    {
                        throw new ExpressionParseException("Paréntesis sin pareja.", token.Position);
                    }

                    break;
            }
        }

        while (operators.Count > 0)
        {
            var op = operators.Pop();
            if (op.Type is TokenType.LeftParen or TokenType.RightParen)
            {
                throw new ExpressionParseException("Paréntesis sin cerrar.", op.Position);
            }

            output.Add(new ParsedExpression.RpnToken(ParsedExpression.RpnTokenType.Operator, 0, op.Operator));
        }

        return output;
    }

    private static bool HasHigherPrecedence(char opA, char opB) =>
        GetPrecedence(opA) > GetPrecedence(opB);

    private static bool HasEqualPrecedence(char opA, char opB) =>
        GetPrecedence(opA) == GetPrecedence(opB);

    private static bool IsRightAssociative(char op) => op == '^';

    private static int GetPrecedence(char op) =>
        op switch
        {
            '+' or '-' => 1,
            '*' or '/' => 2,
            '^' => 3,
            _ => 0
        };

    private static IReadOnlyList<Token> Tokenize(string raw)
    {
        var tokens = new List<Token>();
        var span = raw.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                var start = i;
                var hasDecimal = c == '.';

                while (i + 1 < span.Length)
                {
                    var next = span[i + 1];
                    if (char.IsDigit(next))
                    {
                        i++;
                        continue;
                    }

                    if (next == '.' && !hasDecimal)
                    {
                        hasDecimal = true;
                        i++;
                        continue;
                    }

                    break;
                }

                var slice = span.Slice(start, i - start + 1);
                if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    throw new ExpressionParseException("Número inválido.", start);
                }

                tokens.Add(Token.NumberToken(value, start));
                continue;
            }

            if (c == 'q' || c == 'Q')
            {
                tokens.Add(Token.Variable(i));
                continue;
            }

            if (c == '(')
            {
                tokens.Add(Token.LeftParen(i));
                continue;
            }

            if (c == ')')
            {
                tokens.Add(Token.RightParen(i));
                continue;
            }

            if (IsOperator(c))
            {
                var isUnary = IsUnary(tokens);
                if (isUnary)
                {
                    if (c == '+')
                    {
                        continue;
                    }

                    if (c == '-')
                    {
                        tokens.Add(Token.NumberToken(0, i));
                    }
                }

                tokens.Add(Token.OperatorToken(c, i));
                continue;
            }

            throw new ExpressionParseException($"Carácter inesperado '{c}'.", i);
        }

        return InsertImplicitMultiplication(tokens);
    }

    private static bool IsOperator(char c) => c is '+' or '-' or '*' or '/' or '^';

    private static bool IsUnary(List<Token> tokens) =>
        tokens.Count == 0 ||
        tokens[^1].Type is TokenType.Operator or TokenType.LeftParen;

    private static IReadOnlyList<Token> InsertImplicitMultiplication(List<Token> tokens)
    {
        if (tokens.Count < 2)
        {
            return tokens;
        }

        var result = new List<Token>(tokens.Count * 2);
        for (var i = 0; i < tokens.Count; i++)
        {
            var current = tokens[i];
            result.Add(current);

            if (i == tokens.Count - 1)
            {
                break;
            }

            var next = tokens[i + 1];
            if (NeedsImplicitMultiplication(current.Type, next.Type))
            {
                result.Add(Token.OperatorToken('*', next.Position));
            }
        }

        return result;
    }

    private static bool NeedsImplicitMultiplication(TokenType left, TokenType right) =>
        CanEndImplicit(left) && CanStartImplicit(right);

    private static bool CanEndImplicit(TokenType type) =>
        type is TokenType.Number or TokenType.Variable or TokenType.RightParen;

    private static bool CanStartImplicit(TokenType type) =>
        type is TokenType.Number or TokenType.Variable or TokenType.LeftParen;
}
