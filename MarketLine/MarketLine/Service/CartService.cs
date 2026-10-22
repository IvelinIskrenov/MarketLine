using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        public async Task<string?> AddAsync(int itemId, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var item = await _context.Items.FirstOrDefaultAsync(i => i.id == itemId);
            if (item == null) return "Продуктът не е намерен.";
            if (item.stock_quantity <= 0) return "Продуктът е изчерпан.";

            var state = LoadState();
            var line = state.Lines.FirstOrDefault(l => l.ItemId == itemId);
            var newQuantity = (line?.Quantity ?? 0) + quantity;

            if (newQuantity > item.stock_quantity)
            {
                return $"Налични са само {item.stock_quantity} бр.";
            }

            if (line == null)
            {
                state.Lines.Add(new CartStateLine { ItemId = itemId, Quantity = newQuantity });
            }
            else
            {
                line.Quantity = newQuantity;
            }

            SaveState(state);
            return null;
        }

        public Task RemoveAsync(int itemId)
        {
            var state = LoadState();
            state.Lines.RemoveAll(l => l.ItemId == itemId);
            SaveState(state);
            return Task.CompletedTask;
        }

        public async Task<string?> UpdateQuantityAsync(int itemId, int quantity)
        {
            if (quantity < 1)
            {
                await RemoveAsync(itemId);
                return null;
            }

            var item = await _context.Items.FirstOrDefaultAsync(i => i.id == itemId);
            if (item == null) return "Продуктът не е намерен.";
            if (quantity > item.stock_quantity)
            {
                return $"Налични са само {item.stock_quantity} бр.";
            }

            var state = LoadState();
            var line = state.Lines.FirstOrDefault(l => l.ItemId == itemId);
            if (line == null)
            {
                state.Lines.Add(new CartStateLine { ItemId = itemId, Quantity = quantity });
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

            if (string.IsNullOrWhiteSpace(code))
            {
                return "Въведете промоционален код.";
            }

            if (!TryGetPromoDiscount(code, cart.SubTotal, out _, out var error) && error != null)
            {
                return error;
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
            var itemIds = state.Lines.Select(l => l.ItemId).ToList();
            var items = await _context.Items
                .Where(i => itemIds.Contains(i.id))
                .ToListAsync();

            var lines = new List<CartLine>();
            foreach (var savedLine in state.Lines.ToList())
            {
                var item = items.FirstOrDefault(i => i.id == savedLine.ItemId);
                if (item == null)
                {
                    state.Lines.RemoveAll(l => l.ItemId == savedLine.ItemId);
                    continue;
                }

                var quantity = Math.Min(savedLine.Quantity, item.stock_quantity);
                if (quantity < 1)
                {
                    state.Lines.RemoveAll(l => l.ItemId == savedLine.ItemId);
                    continue;
                }

                savedLine.Quantity = quantity;
                lines.Add(new CartLine
                {
                    ItemId = item.id,
                    Name = item.name,
                    ImageUrl = item.imageUrl,
                    UnitPrice = item.DiscountedPrice,
                    Quantity = quantity,
                    StockQuantity = item.stock_quantity
                });
            }

            SaveState(state);

            var subTotal = lines.Sum(l => l.LineTotal);
            if (!TryGetPromoDiscount(state.PromoCode, subTotal, out var promoDiscount, out _) && !string.IsNullOrWhiteSpace(state.PromoCode))
            {
                state.PromoCode = null;
                promoDiscount = 0;
                SaveState(state);
            }
            var shipping = subTotal >= FreeShippingThreshold ? 0m : (lines.Count == 0 ? 0m : ShippingCost);
            var remaining = subTotal >= FreeShippingThreshold ? 0m : FreeShippingThreshold - subTotal;

            return new CartViewModel
            {
                Lines = lines,
                PromoCode = state.PromoCode,
                SubTotal = subTotal,
                PromoDiscount = promoDiscount,
                Shipping = shipping,
                Total = Math.Max(0, subTotal - promoDiscount + shipping),
                FreeShippingThreshold = FreeShippingThreshold,
                RemainingForFreeShipping = remaining
            };
        }

        private static bool TryGetPromoDiscount(string? code, decimal subTotal, out decimal discount, out string? error)
        {
            discount = 0;
            error = null;

            if (string.IsNullOrWhiteSpace(code))
            {
                return true;
            }

            switch (code.Trim().ToUpperInvariant())
            {
                case "WELCOME10":
                    discount = Math.Round(subTotal * 0.10m, 2);
                    return true;
                case "SAVE5":
                    if (subTotal < 5m)
                    {
                        error = "Код SAVE5 изисква поръчка над 5.00.";
                        return false;
                    }
                    discount = 5m;
                    return true;
                default:
                    error = "Невалиден промоционален код.";
                    return false;
            }
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
            public int Quantity { get; set; }
        }
    }
}
