using System.Globalization;
using System.Text;
using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class SalesReportService : ISalesReportService
    {
        private static readonly CultureInfo Bg = CultureInfo.GetCultureInfo("bg-BG");
        private readonly MarketLineDbContext _context;

        public SalesReportService(MarketLineDbContext context)
        {
            _context = context;
        }

        public async Task<SalesReport> GetAsync(string? period, DateTime? from, DateTime? to)
        {
            var range = ResolveRange(period, from, to);
            var query = _context.Orders.Include(order => order.Lines).AsQueryable();
            if (range.Period != SalesReportPeriods.All)
            {
                query = query.Where(order => order.CreatedAt >= range.From && order.CreatedAt < range.ToExclusive);
            }

            var orders = await query
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();

            if (range.Period == SalesReportPeriods.All)
            {
                var first = orders.Count == 0 ? range.To : orders.Min(order => order.CreatedAt).Date;
                range = range with { From = first };
            }

            var active = orders
                .Where(order => OrderStatuses.Normalize(order.Status) != OrderStatuses.Cancelled)
                .ToList();

            var report = new SalesReport
            {
                Period = range.Period,
                From = range.From,
                To = range.To,
                PeriodLabel = range.Label,
                OrderCount = active.Count,
                CancelledCount = orders.Count - active.Count,
                ItemsSold = active.SelectMany(order => order.Lines).Sum(line => line.Quantity),
                Turnover = active.Sum(order => order.Total),
                PaidTurnover = active
                    .Where(order => order.PaymentStatus == PaymentStatuses.Paid)
                    .Sum(order => order.Total),
                DiscountTotal = active.Sum(order => order.PromoDiscount),
                ShippingTotal = active.Sum(order => order.Shipping),
                Orders = orders,
                Products = active
                    .SelectMany(order => order.Lines)
                    .GroupBy(line => new { line.ItemId, line.Name })
                    .Select(group => new SalesReportProduct
                    {
                        ItemId = group.Key.ItemId,
                        Name = group.Key.Name,
                        Quantity = group.Sum(line => line.Quantity),
                        Total = group.Sum(line => line.LineTotal)
                    })
                    .OrderByDescending(product => product.Total)
                    .ThenBy(product => product.Name)
                    .ToList(),
                Periods = BuildBuckets(active, range.From, range.ToExclusive)
            };

            report.AverageOrder = report.OrderCount == 0
                ? 0
                : Math.Round(report.Turnover / report.OrderCount, 2);

            return report;
        }

        public string ToCsv(SalesReport report)
        {
            var csv = new StringBuilder();
            csv.AppendLine(Csv("Показател", "Стойност"));
            csv.AppendLine(Csv("Период", report.PeriodLabel));
            csv.AppendLine(Csv("От", report.From.ToString("dd.MM.yyyy")));
            csv.AppendLine(Csv("До", report.To.ToString("dd.MM.yyyy")));
            csv.AppendLine(Csv("Поръчки", report.OrderCount.ToString(Bg)));
            csv.AppendLine(Csv("Отказани", report.CancelledCount.ToString(Bg)));
            csv.AppendLine(Csv("Продадени бройки", report.ItemsSold.ToString(Bg)));
            csv.AppendLine(Csv("Оборот", report.Turnover.ToString("0.00", Bg)));
            csv.AppendLine(Csv("Средна поръчка", report.AverageOrder.ToString("0.00", Bg)));
            csv.AppendLine(Csv("Платено", report.PaidTurnover.ToString("0.00", Bg)));
            csv.AppendLine(Csv("Отстъпки", report.DiscountTotal.ToString("0.00", Bg)));
            csv.AppendLine(Csv("Доставки", report.ShippingTotal.ToString("0.00", Bg)));
            csv.AppendLine();
            csv.AppendLine(Csv("Период", "Поръчки", "Оборот"));
            foreach (var bucket in report.Periods)
            {
                csv.AppendLine(Csv(bucket.Label, bucket.OrderCount.ToString(Bg), bucket.Turnover.ToString("0.00", Bg)));
            }

            csv.AppendLine();
            csv.AppendLine(Csv("Продукт", "Количество", "Оборот"));
            foreach (var product in report.Products)
            {
                csv.AppendLine(Csv(product.Name, product.Quantity.ToString(Bg), product.Total.ToString("0.00", Bg)));
            }

            csv.AppendLine();
            csv.AppendLine(Csv("Номер", "Дата", "Клиент", "Статус", "Плащане", "Сума"));
            foreach (var order in report.Orders)
            {
                csv.AppendLine(Csv(
                    order.OrderNumber,
                    order.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    $"{order.FirstName} {order.LastName}",
                    OrderStatuses.Normalize(order.Status),
                    order.PaymentStatus,
                    order.Total.ToString("0.00", Bg)));
            }

            return csv.ToString();
        }

        internal static ReportRange ResolveRange(string? period, DateTime? from, DateTime? to)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var key = (period ?? SalesReportPeriods.Month).Trim().ToLowerInvariant();

            if (key == SalesReportPeriods.Custom)
            {
                if (from == null || to == null)
                {
                    key = SalesReportPeriods.Month;
                }
                else
                {
                    var start = from.Value.Date;
                    var end = to.Value.Date;
                    if (start > end)
                    {
                        (start, end) = (end, start);
                    }

                    var earliest = today.AddYears(-10);
                    if (start < earliest)
                    {
                        start = earliest;
                    }

                    if (end > today)
                    {
                        end = today;
                    }

                    if (start > end)
                    {
                        start = end;
                    }

                    return new ReportRange(
                        SalesReportPeriods.Custom,
                        start,
                        end,
                        end.AddDays(1),
                        $"{start:dd.MM.yyyy} – {end:dd.MM.yyyy}");
                }
            }

            return key switch
            {
                SalesReportPeriods.Today => new ReportRange(key, today, today, today.AddDays(1), "Днес"),
                SalesReportPeriods.Week => new ReportRange(key, today.AddDays(-6), today, today.AddDays(1), "Последните 7 дни"),
                SalesReportPeriods.Year => new ReportRange(
                    key,
                    new DateTime(today.Year, 1, 1),
                    today,
                    today.AddDays(1),
                    today.Year.ToString(Bg)),
                SalesReportPeriods.All => new ReportRange(key, today.AddYears(-10), today, today.AddDays(1), "Всички"),
                _ => new ReportRange(
                    SalesReportPeriods.Month,
                    new DateTime(today.Year, today.Month, 1),
                    today,
                    today.AddDays(1),
                    today.ToString("MMMM yyyy", Bg))
            };
        }

        private static List<SalesReportBucket> BuildBuckets(List<Order> orders, DateTime from, DateTime toExclusive)
        {
            var days = (toExclusive - from).TotalDays;
            if (days <= 31)
            {
                return FillBuckets(
                    orders,
                    from,
                    toExclusive,
                    date => date.Date,
                    date => date.AddDays(1),
                    date => date.ToString("dd.MM.yyyy"));
            }

            if (days <= 370)
            {
                var start = new DateTime(from.Year, from.Month, 1);
                return FillBuckets(
                    orders,
                    start,
                    toExclusive,
                    date => new DateTime(date.Year, date.Month, 1),
                    date => date.AddMonths(1),
                    date => date.ToString("MMMM yyyy", Bg));
            }

            var yearStart = new DateTime(from.Year, 1, 1);
            return FillBuckets(
                orders,
                yearStart,
                toExclusive,
                date => new DateTime(date.Year, 1, 1),
                date => date.AddYears(1),
                date => date.ToString("yyyy"));
        }

        private static List<SalesReportBucket> FillBuckets(
            List<Order> orders,
            DateTime from,
            DateTime toExclusive,
            Func<DateTime, DateTime> keyOf,
            Func<DateTime, DateTime> next,
            Func<DateTime, string> label)
        {
            var groups = orders
                .GroupBy(order => keyOf(order.CreatedAt))
                .ToDictionary(
                    group => group.Key,
                    group => (Count: group.Count(), Total: group.Sum(order => order.Total)));

            var buckets = new List<SalesReportBucket>();
            for (var current = keyOf(from); current < toExclusive; current = next(current))
            {
                groups.TryGetValue(current, out var stats);
                buckets.Add(new SalesReportBucket
                {
                    Date = current,
                    Label = label(current),
                    OrderCount = stats.Count,
                    Turnover = stats.Total
                });
            }

            return buckets;
        }

        private static string Csv(params string[] values)
        {
            return string.Join(';', values.Select(Escape));
        }

        private static string Escape(string? value)
        {
            value ??= string.Empty;
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        internal readonly record struct ReportRange(
            string Period,
            DateTime From,
            DateTime To,
            DateTime ToExclusive,
            string Label);
    }
}
