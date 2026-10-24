using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IShopAdminService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task AddCategoryAsync(string name, int? parentId);
        Task DeleteCategoryAsync(int id);

        Task<List<PromoCode>> GetPromosAsync();
        Task AddPromoAsync(PromoCode promo);
        Task TogglePromoAsync(int id);
        Task DeletePromoAsync(int id);

        Task<List<Courier>> GetCouriersAsync(bool activeOnly = false);
        Task AddCourierAsync(Courier courier);
        Task ToggleCourierAsync(int id);
        Task DeleteCourierAsync(int id);

        Task<ShippingSettings> GetShippingAsync();
        Task SaveShippingAsync(ShippingSettings settings);

        Task<List<SitePage>> GetPagesAsync();
        Task<SitePage?> GetPageAsync(string key);
        Task SavePageAsync(SitePage page);

        Task UpdateStockAsync(int itemId, int quantity);

        Task<AdminStats> GetStatsAsync();
        Task<List<User>> GetCustomersAsync();
        Task<AdminCustomer?> GetCustomerAsync(string userId);
    }

    public class AdminStats
    {
        public int OrderCount { get; set; }
        public decimal Turnover { get; set; }
        public decimal AverageOrder { get; set; }
        public List<AdminTopProduct> TopProducts { get; set; } = new();
    }

    public class AdminTopProduct
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }

    public class AdminCustomer
    {
        public User User { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }
}
