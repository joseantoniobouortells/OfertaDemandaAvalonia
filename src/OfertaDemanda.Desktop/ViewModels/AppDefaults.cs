using OfertaDemanda.Core.Models;

namespace OfertaDemanda.Desktop.ViewModels;

internal static class AppDefaults
{
    internal static class Market
    {
        public const string DemandExpression = "100 - 0.5q";
        public const string SupplyExpression = "20 + 0.5q";
        public const double DemandShock = 0;
        public const double SupplyShock = 0;
        public const double Tax = 0;
    }

    internal static class Firm
    {
        public const string CostExpression = "200 + 10q + 0.5q^2";
        public const double Price = 40;
        public const FirmMode Mode = FirmMode.ShortRun;
    }

    internal static class Monopoly
    {
        public const string DemandExpression = "120 - q";
        public const string CostExpression = "100 + 10q + 0.2q^2";
    }

    internal static class Elasticity
    {
        public const double Price = 50;
    }
}
