using MarketLine.Models;
using System.Text.Json;

namespace MarketLine.Services
{
    public class CheckoutService : ICheckoutService
    {
        private const string DraftKey = "CheckoutDraft";
        private const string OrderKey = "PlacedOrder";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckoutService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SaveDraft(CheckoutForm form)
        {
            SetJson(DraftKey, form);
        }

        public CheckoutForm? GetDraft()
        {
            return GetJson<CheckoutForm>(DraftKey);
        }

        public PlacedOrder PlaceOrder(CheckoutForm form, CartViewModel cart)
        {
            var order = new PlacedOrder
            {
                OrderNumber = $"ML-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                CreatedAt = DateTime.Now,
                Customer = form,
                Lines = cart.Lines.ToList(),
                PromoCode = cart.PromoCode,
                SubTotal = cart.SubTotal,
                PromoDiscount = cart.PromoDiscount,
                Shipping = cart.Shipping,
                Total = cart.Total
            };

            SetJson(OrderKey, order);
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Remove(DraftKey);
            return order;
        }

        public PlacedOrder? GetOrder(string orderNumber)
        {
            var order = GetJson<PlacedOrder>(OrderKey);
            if (order == null || !string.Equals(order.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return order;
        }

        private T? GetJson<T>(string key)
        {
            var json = _httpContextAccessor.HttpContext?.Session.GetString(key);
            if (string.IsNullOrEmpty(json)) return default;
            return JsonSerializer.Deserialize<T>(json);
        }

        private void SetJson<T>(string key, T value)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;
            session.SetString(key, JsonSerializer.Serialize(value));
        }
    }
}
