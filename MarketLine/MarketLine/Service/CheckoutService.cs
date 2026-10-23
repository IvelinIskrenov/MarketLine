using MarketLine.Models;
using System.Text.Json;

namespace MarketLine.Services
{
    public class CheckoutService : ICheckoutService
    {
        private const string DraftKey = "CheckoutDraft";

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

        public void ClearDraft()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(DraftKey);
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
