using System.Globalization;

namespace MarketLine.Localization
{
    public static class Money
    {
        public static string Euro(this decimal value)
        {
            return "€" + value.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
