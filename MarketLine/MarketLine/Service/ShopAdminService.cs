using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class ShopAdminService : IShopAdminService
    {
        private readonly MarketLineDbContext _context;

        public ShopAdminService(MarketLineDbContext context)
        {
            _context = context;
        }

        public Task<List<Category>> GetCategoriesAsync()
        {
            return _context.Categories
                .Include(c => c.Parent)
                .OrderBy(c => c.ParentId)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task AddCategoryAsync(string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            _context.Categories.Add(new Category
            {
                Name = name.Trim(),
                ParentId = parentId
            });
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null || category.Id <= 3) return;

            foreach (var child in category.Children)
            {
                child.ParentId = category.ParentId;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public Task<List<PromoCode>> GetPromosAsync()
        {
            return _context.PromoCodes.OrderBy(p => p.Code).ToListAsync();
        }

        public async Task AddPromoAsync(PromoCode promo)
        {
            promo.Code = promo.Code.Trim().ToUpperInvariant();
            _context.PromoCodes.Add(promo);
            await _context.SaveChangesAsync();
        }

        public async Task TogglePromoAsync(int id)
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return;
            promo.IsActive = !promo.IsActive;
            await _context.SaveChangesAsync();
        }

        public async Task DeletePromoAsync(int id)
        {
            var promo = await _context.PromoCodes.FindAsync(id);
            if (promo == null) return;
            _context.PromoCodes.Remove(promo);
            await _context.SaveChangesAsync();
        }

        public Task<List<Courier>> GetCouriersAsync(bool activeOnly = false)
        {
            var query = _context.Couriers.AsQueryable();
            if (activeOnly) query = query.Where(c => c.IsActive);
            return query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task AddCourierAsync(Courier courier)
        {
            courier.Name = courier.Name.Trim();
            _context.Couriers.Add(courier);
            await _context.SaveChangesAsync();
        }

        public async Task ToggleCourierAsync(int id)
        {
            var courier = await _context.Couriers.FindAsync(id);
            if (courier == null) return;
            courier.IsActive = !courier.IsActive;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCourierAsync(int id)
        {
            var courier = await _context.Couriers.FindAsync(id);
            if (courier == null) return;
            _context.Couriers.Remove(courier);
            await _context.SaveChangesAsync();
        }

        public async Task<ShippingSettings> GetShippingAsync()
        {
            var settings = await _context.ShippingSettings.FirstOrDefaultAsync();
            if (settings != null) return settings;

            settings = new ShippingSettings { DefaultPrice = 5.99m, FreeOver = 100m };
            _context.ShippingSettings.Add(settings);
            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task SaveShippingAsync(ShippingSettings settings)
        {
            var current = await GetShippingAsync();
            current.DefaultPrice = settings.DefaultPrice;
            current.FreeOver = settings.FreeOver;
            await _context.SaveChangesAsync();
        }

        public Task<List<SitePage>> GetPagesAsync()
        {
            return _context.SitePages.OrderBy(p => p.Title).ToListAsync();
        }

        public Task<SitePage?> GetPageAsync(string key)
        {
            return _context.SitePages.FirstOrDefaultAsync(p => p.Key == key);
        }

        public async Task SavePageAsync(SitePage page)
        {
            var current = await _context.SitePages.FirstOrDefaultAsync(p => p.Id == page.Id || p.Key == page.Key);
            if (current == null)
            {
                _context.SitePages.Add(page);
            }
            else
            {
                current.Title = page.Title;
                current.Body = page.Body;
                current.ImageUrl = page.ImageUrl;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStockAsync(int itemId, int quantity)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null) return;
            item.stock_quantity = Math.Max(0, quantity);
            await _context.SaveChangesAsync();
        }

        public async Task<AdminStats> GetStatsAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Lines)
                .Where(o => o.Status != OrderStatuses.Cancelled)
                .ToListAsync();

            var stats = new AdminStats
            {
                OrderCount = orders.Count,
                Turnover = orders.Sum(o => o.Total)
            };
            stats.AverageOrder = stats.OrderCount == 0 ? 0 : Math.Round(stats.Turnover / stats.OrderCount, 2);
            stats.TopProducts = orders
                .SelectMany(o => o.Lines)
                .GroupBy(l => l.Name)
                .Select(g => new AdminTopProduct
                {
                    Name = g.Key,
                    Quantity = g.Sum(l => l.Quantity),
                    Total = g.Sum(l => l.LineTotal)
                })
                .OrderByDescending(p => p.Quantity)
                .Take(5)
                .ToList();

            return stats;
        }

        public Task<List<User>> GetCustomersAsync()
        {
            return _context.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
        }

        public async Task<AdminCustomer?> GetCustomerAsync(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var orders = await _context.Orders
                .Include(o => o.Lines)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return new AdminCustomer { User = user, Orders = orders };
        }
    }
}
