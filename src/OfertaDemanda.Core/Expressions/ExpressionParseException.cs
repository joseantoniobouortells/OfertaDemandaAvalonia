using System;

namespace OfertaDemanda.Core.Expressions;

/// <summary>
/// Exception thrown when an expression cannot be parsed.
/// </summary>
public class ExpressionParseException : Exception
{
    public int Position { get; }

    public ExpressionParseException(string message, int position)
        : base($"{message} (pos {position})")
    {
        Position = position;
    }

    public ExpressionParseException(string message)
        : base(message)
    {
        Position = -1;
    }
}
