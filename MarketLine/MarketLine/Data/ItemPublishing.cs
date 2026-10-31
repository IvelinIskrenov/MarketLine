using System.Globalization;
using MarketLine.Models;

namespace MarketLine.Data
{
    public static class ItemPublishing
    {
        public static IQueryable<Item> WhereLive(this IQueryable<Item> items)
        {
            var now = DateTime.UtcNow;
            return items.Where(item => item.PublishedAt == null || item.PublishedAt <= now);
        }

        public static bool IsLive(Item? item)
        {
            return item != null && !item.IsScheduled;
        }

        public static DateTime AsUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public static string? ToLocalInput(DateTime? publishedAt)
        {
            if (!publishedAt.HasValue)
            {
                return null;
            }

            return AsUtc(publishedAt.Value).ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm");
        }

        public static string? ToLocalDisplay(DateTime? publishedAt)
        {
            if (!publishedAt.HasValue)
            {
                return null;
            }

            return AsUtc(publishedAt.Value).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        }

        public static bool TryParseLocal(string? input, out DateTime? publishedAt, out string? error)
        {
            publishedAt = null;
            error = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return true;
            }

            var formats = new[] { "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd HH:mm" };
            if (!DateTime.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)
                && !DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out local))
            {
                error = "Невалидна дата за публикуване.";
                return false;
            }

            publishedAt = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
            return true;
        }
    }
}
