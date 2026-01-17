using OfertaDemanda.Core.Models;

namespace OfertaDemanda.Shared.Settings;

public static class AppDefaults
{
    public static class Market
    {
        public const string DemandExpression = "100 - 0.5q";
        public const string SupplyExpression = "20 + 0.5q";
        public const double DemandShock = 0;
        public const double SupplyShock = 0;
        public const double Tax = 0;
        public const MarketCostFunctionType CostType = MarketCostFunctionType.Quadratic;
        public const double FixedCost = 50;
        public const double LinearCost = 8;
        public const double QuadraticCost = 0.4;
        public const double CubicCost = 0.01;
    }

    public static class Firm
    {
        public const string CostExpression = "200 + 10q + 0.5q^2";
        public const double Price = 40;
        public const FirmMode Mode = FirmMode.ShortRun;
    }

    public static class Monopoly
    {
        public const string DemandExpression = "120 - q";
        public const string CostExpression = "100 + 10q + 0.2q^2";
    }

    public static class Elasticity
    {
        public const double Price = 50;
        public const string SupplyExpression = "20 + 0.5q";
    }
}
