namespace OfertaDemanda.Core.Expressions;

internal enum TokenType
{
    Number,
    Variable,
    Operator,
    LeftParen,
    RightParen
}

internal readonly record struct Token(TokenType Type, double Number, char Operator, int Position)
{
    public static Token NumberToken(double value, int position) => new(TokenType.Number, value, '\0', position);
    public static Token Variable(int position) => new(TokenType.Variable, 0, 'q', position);
    public static Token OperatorToken(char op, int position) => new(TokenType.Operator, 0, op, position);
    public static Token LeftParen(int position) => new(TokenType.LeftParen, 0, '(', position);
    public static Token RightParen(int position) => new(TokenType.RightParen, 0, ')', position);

    public override string ToString() =>
        Type switch
        {
            TokenType.Number => Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            TokenType.Variable => "q",
            TokenType.Operator => Operator.ToString(),
            TokenType.LeftParen => "(",
            TokenType.RightParen => ")",
            _ => Type.ToString()
        };
}
