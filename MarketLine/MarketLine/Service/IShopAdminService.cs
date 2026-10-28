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
        Task UpdateVariantStockAsync(int variantId, int quantity);
        Task AddVariantAsync(int itemId, string? size, string? color, string? model, int quantity);
        Task DeleteVariantAsync(int variantId);

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
        public List<AdminStockAlert> LowStockItems { get; set; } = new();
    }

    public class AdminTopProduct
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }

    public class AdminStockAlert
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? VariantLabel { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSoldOut => StockQuantity <= 0;
    }

    public class AdminCustomer
    {
        public User User { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }
}
