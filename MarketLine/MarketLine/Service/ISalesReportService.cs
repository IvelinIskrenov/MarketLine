using MarketLine.Models;

namespace MarketLine.Services
{
    public static class SalesReportPeriods
    {
        public const string Today = "today";
        public const string Week = "week";
        public const string Month = "month";
        public const string Year = "year";
        public const string All = "all";
        public const string Custom = "custom";

        public static readonly (string Key, string Label)[] Presets =
        {
            (Today, "Днес"),
            (Week, "7 дни"),
            (Month, "Месец"),
            (Year, "Година"),
            (All, "Всички")
        };
    }

    public class SalesReport
    {
        public string Period { get; set; } = SalesReportPeriods.Month;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public int CancelledCount { get; set; }
        public int ItemsSold { get; set; }
        public decimal Turnover { get; set; }
        public decimal AverageOrder { get; set; }
        public decimal PaidTurnover { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal ShippingTotal { get; set; }
        public List<SalesReportBucket> Periods { get; set; } = new();
        public List<SalesReportProduct> Products { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }

    public class SalesReportBucket
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Turnover { get; set; }
    }

    public class SalesReportProduct
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }

    public interface ISalesReportService
    {
        Task<SalesReport> GetAsync(string? period, DateTime? from, DateTime? to);
        string ToCsv(SalesReport report);
    }
}
