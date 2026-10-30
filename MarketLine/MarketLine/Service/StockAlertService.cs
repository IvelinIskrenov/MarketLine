using System.ComponentModel.DataAnnotations;
using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Security;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class StockAlertService : IStockAlertService
    {
        private readonly MarketLineDbContext _context;
        private readonly IEmailSender _email;
        private readonly IConfiguration _config;

        public StockAlertService(MarketLineDbContext context, IEmailSender email, IConfiguration config)
        {
            _context = context;
            _email = email;
            _config = config;
        }

        public async Task<string?> SubscribeAsync(int itemId, int variantId, string? email, string? userId)
        {
            email = NormalizeEmail(email);
            if (email == null)
            {
                return "Въведете валиден имейл.";
            }

            var item = await _context.Items
                .Include(current => current.Variants)
                .FirstOrDefaultAsync(current => current.id == itemId);
            if (item == null)
            {
                return "Продуктът не е намерен.";
            }

            ItemVariant? variant = null;
            if (variantId > 0)
            {
                variant = item.Variants.FirstOrDefault(current => current.Id == variantId);
                if (variant == null)
                {
                    return "Вариантът не е намерен.";
                }

                if (!variant.IsSoldOut)
                {
                    return "Този вариант вече е в наличност.";
                }
            }
            else if (!item.IsSoldOut)
            {
                return "Продуктът вече е в наличност.";
            }

            var existing = await _context.StockAlerts.FirstOrDefaultAsync(alert =>
                alert.ItemId == itemId
                && alert.VariantId == (variant == null ? null : variant.Id)
                && alert.Email == email
                && alert.NotifiedAt == null);
            if (existing != null)
            {
                return null;
            }

            _context.StockAlerts.Add(new StockAlert
            {
                ItemId = itemId,
                VariantId = variant?.Id,
                Email = email,
                UserId = userId,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<int> NotifyRestockAsync(int itemId)
        {
            var item = await _context.Items
                .Include(current => current.Variants)
                .FirstOrDefaultAsync(current => current.id == itemId);
            if (item == null)
            {
                return 0;
            }

            var pending = await _context.StockAlerts
                .Where(alert => alert.ItemId == itemId && alert.NotifiedAt == null)
                .ToListAsync();
            if (pending.Count == 0)
            {
                return 0;
            }

            var sent = 0;
            var now = DateTime.Now;
            foreach (var alert in pending)
            {
                if (!IsAvailable(item, alert.VariantId))
                {
                    continue;
                }

                var variant = alert.VariantId is int variantId
                    ? item.Variants.FirstOrDefault(current => current.Id == variantId)
                    : null;
                await _email.SendAsync(alert.Email, BuildSubject(item), BuildBody(item, variant));
                alert.NotifiedAt = now;
                sent++;
            }

            if (sent > 0)
            {
                await _context.SaveChangesAsync();
            }

            return sent;
        }

        public Task<List<StockAlert>> GetRecentAsync()
        {
            return _context.StockAlerts
                .Include(alert => alert.Item)
                .Include(alert => alert.Variant)
                .OrderBy(alert => alert.NotifiedAt.HasValue)
                .ThenByDescending(alert => alert.CreatedAt)
                .Take(200)
                .ToListAsync();
        }

        private static bool IsAvailable(Item item, int? variantId)
        {
            if (variantId is int id)
            {
                var variant = item.Variants.FirstOrDefault(current => current.Id == id);
                return variant != null && variant.StockQuantity > 0;
            }

            return item.AvailableStock > 0;
        }

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            email = email.Trim().ToLowerInvariant();
            if (email.Length > InputLimits.Email || !new EmailAddressAttribute().IsValid(email))
            {
                return null;
            }

            return email;
        }

        private static string BuildSubject(Item item)
        {
            return $"{item.name} отново е в наличност";
        }

        private string BuildBody(Item item, ItemVariant? variant)
        {
            var baseUrl = (_config["Email:PublicBaseUrl"] ?? "http://127.0.0.1:5045").TrimEnd('/');
            var lines = new List<string>
            {
                "Здравейте,",
                string.Empty,
                $"Продуктът \"{item.name}\" отново е в наличност."
            };

            if (variant != null)
            {
                lines.Add($"Вариант: {variant.Label}");
            }

            lines.Add(string.Empty);
            lines.Add($"Вижте продукта: {baseUrl}/Item/Details/{item.id}");
            lines.Add(string.Empty);
            lines.Add("Поздрави,");
            lines.Add("MarketLine");
            return string.Join(Environment.NewLine, lines);
        }
    }
}
