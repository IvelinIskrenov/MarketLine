using System.Text.Json;
using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Security;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class CartService : ICartService
    {
        public const decimal FreeShippingThreshold = 100m;
        public const decimal ShippingCost = 5.99m;

        private const string SessionKey = "Cart";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MarketLineDbContext _context;

        public CartService(IHttpContextAccessor httpContextAccessor, MarketLineDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<CartViewModel> GetCartAsync()
        {
            var state = LoadState();
            return await BuildCartAsync(state);
        }

        public async Task<string?> AddAsync(int itemId, int quantity = 1, int variantId = 0)
        {
            if (itemId <= 0)
            {
                return "Невалиден продукт.";
            }

            if (quantity < 1 || quantity > InputLimits.QuantityMax)
            {
                return "Невалидно количество.";
            }

            var item = await LoadItemAsync(itemId);
            if (item == null) return "Продуктът не е намерен.";

            var variant = ResolveVariant(item, variantId);
            if (item.Variants.Count > 0 && variant == null)
            {
                return "Изберете размер, цвят или модел.";
            }

            var available = variant?.StockQuantity ?? item.stock_quantity;
            if (available <= 0)
            {
                return "Продуктът е изчерпан.";
            }

            var state = LoadState();
            var resolvedVariantId = variant?.Id ?? 0;
            var line = FindLine(state, itemId, resolvedVariantId);
            var newQuantity = (line?.Quantity ?? 0) + quantity;

            if (newQuantity > available)
            {
                return $"Налични са само {available} бр.";
            }

            if (line == null)
            {
                state.Lines.Add(new CartStateLine
                {
                    ItemId = itemId,
                    VariantId = resolvedVariantId,
                    Quantity = newQuantity
                });
            }
            else
            {
                line.Quantity = newQuantity;
            }

            SaveState(state);
            return null;
        }

        public Task RemoveAsync(int itemId, int variantId = 0)
        {
            var state = LoadState();
            if (variantId > 0)
            {
                state.Lines.RemoveAll(line => line.ItemId == itemId && line.VariantId == variantId);
            }
            else
            {
                state.Lines.RemoveAll(line => line.ItemId == itemId);
            }

            SaveState(state);
            return Task.CompletedTask;
        }

        public async Task<string?> UpdateQuantityAsync(int itemId, int quantity, int variantId = 0)
        {
            if (itemId <= 0)
            {
                return "Невалиден продукт.";
            }

            if (quantity > InputLimits.QuantityMax)
            {
                return "Невалидно количество.";
            }

            if (quantity < 1)
            {
                await RemoveAsync(itemId, variantId);
                return null;
            }

            var item = await LoadItemAsync(itemId);
            if (item == null) return "Продуктът не е намерен.";

            var variant = ResolveVariant(item, variantId);
            if (item.Variants.Count > 0 && variant == null)
            {
                return "Изберете размер, цвят или модел.";
            }

            var available = variant?.StockQuantity ?? item.stock_quantity;
            if (quantity > available)
            {
                return $"Налични са само {available} бр.";
            }

            var state = LoadState();
            var resolvedVariantId = variant?.Id ?? variantId;
            var line = FindLine(state, itemId, resolvedVariantId);
            if (line == null)
            {
                state.Lines.Add(new CartStateLine
                {
                    ItemId = itemId,
                    VariantId = resolvedVariantId,
                    Quantity = quantity
                });
            }
            else
            {
                line.Quantity = quantity;
            }

            SaveState(state);
            return null;
        }

        public async Task<string?> ApplyPromoAsync(string? code)
        {
            var state = LoadState();
            var cart = await BuildCartAsync(state);

            if (string.IsNullOrWhiteSpace(code) || code.Length > InputLimits.Promo)
            {
                return "Въведете промоционален код.";
            }

            var promoResult = await GetPromoDiscountAsync(code, cart.SubTotal);
            if (promoResult.Error != null)
            {
                return promoResult.Error;
            }

            state.PromoCode = code.Trim().ToUpperInvariant();
            SaveState(state);
            return null;
        }

        public void RemovePromo()
        {
            var state = LoadState();
            state.PromoCode = null;
            SaveState(state);
        }

        public void Clear()
        {
            SaveState(new CartState());
        }

        public int GetCount()
        {
            return LoadState().Lines.Sum(l => l.Quantity);
        }

        private async Task<CartViewModel> BuildCartAsync(CartState state)
        {
            var itemIds = state.Lines.Select(l => l.ItemId).Distinct().ToList();
            var items = await _context.Items
                .Include(i => i.Variants)
                .Where(i => itemIds.Contains(i.id))
                .ToListAsync();

            var lines = new List<CartLine>();
            foreach (var savedLine in state.Lines.ToList())
            {
                var item = items.FirstOrDefault(i => i.id == savedLine.ItemId);
                if (item == null)
                {
                    state.Lines.Remove(savedLine);
                    continue;
                }

                var variant = ResolveVariant(item, savedLine.VariantId);
                if (item.Variants.Count > 0 && variant == null)
                {
                    state.Lines.Remove(savedLine);
                    continue;
                }

                var available = variant?.StockQuantity ?? item.stock_quantity;
                var quantity = Math.Min(savedLine.Quantity, available);
                if (quantity < 1)
                {
                    state.Lines.Remove(savedLine);
                    continue;
                }

                savedLine.Quantity = quantity;
                savedLine.VariantId = variant?.Id ?? 0;
                lines.Add(new CartLine
                {
                    ItemId = item.id,
                    VariantId = savedLine.VariantId,
                    VariantLabel = variant?.Label,
                    Name = item.name,
                    ImageUrl = item.imageUrl,
                    UnitPrice = item.DiscountedPrice,
                    Quantity = quantity,
                    StockQuantity = available
                });
            }

            SaveState(state);

            var subTotal = lines.Sum(l => l.LineTotal);
            var promoResult = await GetPromoDiscountAsync(state.PromoCode, subTotal);
            var promoDiscount = promoResult.Discount;
            if (promoResult.Error != null && !string.IsNullOrWhiteSpace(state.PromoCode))
            {
                state.PromoCode = null;
                promoDiscount = 0;
                SaveState(state);
            }
            var settings = await GetShippingSettingsAsync();
            var shipping = subTotal >= settings.FreeOver ? 0m : (lines.Count == 0 ? 0m : settings.DefaultPrice);
            var remaining = subTotal >= settings.FreeOver ? 0m : settings.FreeOver - subTotal;

            return new CartViewModel
            {
                Lines = lines,
                PromoCode = state.PromoCode,
                SubTotal = subTotal,
                PromoDiscount = promoDiscount,
                Shipping = shipping,
                Total = Math.Max(0, subTotal - promoDiscount + shipping),
                FreeShippingThreshold = settings.FreeOver,
                RemainingForFreeShipping = remaining
            };
        }

        private Task<Item?> LoadItemAsync(int itemId)
        {
            return _context.Items.Include(i => i.Variants).FirstOrDefaultAsync(i => i.id == itemId);
        }

        private static ItemVariant? ResolveVariant(Item item, int variantId)
        {
            return ItemInventoryData.Resolve(item, variantId > 0 ? variantId : null);
        }

        private static CartStateLine? FindLine(CartState state, int itemId, int variantId)
        {
            return variantId > 0
                ? state.Lines.FirstOrDefault(line => line.ItemId == itemId && line.VariantId == variantId)
                : state.Lines.FirstOrDefault(line => line.ItemId == itemId);
        }

        private async Task<(decimal Discount, string? Error)> GetPromoDiscountAsync(string? code, decimal subTotal)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return (0, null);
            }

            var normalized = code.Trim().ToUpperInvariant();
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code == normalized);
            if (promo == null || !promo.IsActive)
            {
                return (0, "Невалиден промоционален код.");
            }

            var now = DateTime.Now;
            if (promo.StartsAt.HasValue && now < promo.StartsAt.Value)
            {
                return (0, "Промоцията още не е активна.");
            }

            if (promo.EndsAt.HasValue && now > promo.EndsAt.Value)
            {
                return (0, "Промоцията е изтекла.");
            }

            if (subTotal < promo.MinSubTotal)
            {
                return (0, $"Код {promo.Code} изисква поръчка над {promo.MinSubTotal:0.00}.");
            }

            var discount = promo.IsPercent
                ? Math.Round(subTotal * promo.Value / 100m, 2)
                : promo.Value;
            return (discount, null);
        }

        private async Task<ShippingSettings> GetShippingSettingsAsync()
        {
            var settings = await _context.ShippingSettings.FirstOrDefaultAsync();
            return settings ?? new ShippingSettings { DefaultPrice = ShippingCost, FreeOver = FreeShippingThreshold };
        }

        private CartState LoadState()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return new CartState();

            var json = session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return new CartState();

            return JsonSerializer.Deserialize<CartState>(json) ?? new CartState();
        }

        private void SaveState(CartState state)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            session.SetString(SessionKey, JsonSerializer.Serialize(state));
        }

        private class CartState
        {
            public List<CartStateLine> Lines { get; set; } = new();
            public string? PromoCode { get; set; }
        }

        private class CartStateLine
        {
            public int ItemId { get; set; }
            public int VariantId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
