using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Security;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class ItemService : IItemService
    {
        private readonly MarketLineDbContext _context;
        private readonly IStockAlertService _alerts;

        public ItemService(MarketLineDbContext context, IStockAlertService alerts)
        {
            _context = context;
            _alerts = alerts;
        }

        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            return await _context.Items
                .Include(i => i.category)
                .Include(i => i.Variants)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetFilteredItemsAsync(string? category)
        {
            var catalog = await GetCatalogAsync(new ItemSearchQuery { Category = category });
            return catalog.Items;
        }

        public async Task<IEnumerable<Item>> GetFilteredItemsAsync(string? category, string? searchTerm)
        {
            var catalog = await GetCatalogAsync(new ItemSearchQuery
            {
                Category = category,
                SearchTerm = searchTerm
            });
            return catalog.Items;
        }

        public async Task<ItemCatalogViewModel> GetCatalogAsync(ItemSearchQuery query)
        {
            query ??= new ItemSearchQuery();

            var allItems = await _context.Items.AsNoTracking().ToListAsync();
            var brands = allItems
                .Select(i => i.Brand)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select(b => b!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(b => b)
                .ToList();
            var features = allItems
                .SelectMany(i => i.FeatureTags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f)
                .ToList();
            var categories = (await GetAllCategoriesAsync()).ToList();

            var itemsQuery = _context.Items
                .Include(i => i.category)
                .ThenInclude(c => c!.Parent)
                .Include(i => i.Variants)
                .AsQueryable();

            itemsQuery = ApplyCategoryFilter(itemsQuery, query.Category);
            itemsQuery = ApplySearchFilter(itemsQuery, query.SearchTerm);

            if (!string.IsNullOrWhiteSpace(query.Brand))
            {
                var brand = query.Brand.Trim().ToLower();
                itemsQuery = itemsQuery.Where(i => i.Brand != null && i.Brand.ToLower() == brand);
            }

            if (!string.IsNullOrWhiteSpace(query.Feature))
            {
                var feature = query.Feature.Trim();
                itemsQuery = itemsQuery.Where(i => i.Features != null && i.Features.Contains(feature));
            }

            var items = await itemsQuery.ToListAsync();

            var minPrice = query.MinPrice;
            var maxPrice = query.MaxPrice;
            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                (minPrice, maxPrice) = (maxPrice, minPrice);
            }

            if (minPrice.HasValue)
            {
                items = items.Where(i => i.DiscountedPrice >= minPrice.Value).ToList();
            }

            if (maxPrice.HasValue)
            {
                items = items.Where(i => i.DiscountedPrice <= maxPrice.Value).ToList();
            }

            items = await SortItemsAsync(items, query.Sort);

            return new ItemCatalogViewModel
            {
                Items = items,
                Query = query,
                Brands = brands,
                Features = features,
                Categories = categories
            };
        }

        public async Task<IReadOnlyList<ItemSuggestion>> SuggestAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            {
                return Array.Empty<ItemSuggestion>();
            }

            var needle = term.Trim().ToLower();
            var matches = await _context.Items
                .AsNoTracking()
                .Where(i =>
                    (i.name != null && i.name.ToLower().Contains(needle)) ||
                    (i.description != null && i.description.ToLower().Contains(needle)) ||
                    (i.Brand != null && i.Brand.ToLower().Contains(needle)))
                .OrderBy(i => i.name)
                .Take(8)
                .Select(i => new ItemSuggestion
                {
                    Id = i.id,
                    Name = i.name ?? string.Empty,
                    Brand = i.Brand
                })
                .ToListAsync();

            return matches;
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Include(i => i.category)
                .Include(i => i.Variants)
                .FirstOrDefaultAsync(m => m.id == id);
        }

        public async Task CreateItemAsync(Item item)
        {
            if (item.CreatedAt == default)
            {
                item.CreatedAt = DateTime.UtcNow;
            }

            _context.Add(item);
            await _context.SaveChangesAsync();
            ItemInventoryData.EnsureVariants(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateItemAsync(Item item)
        {
            var current = await _context.Items
                .Include(i => i.Variants)
                .FirstOrDefaultAsync(i => i.id == item.id);
            if (current == null)
            {
                return;
            }

            current.name = item.name;
            current.description = item.description;
            current.Brand = item.Brand;
            current.Features = item.Features;
            current.discount = item.discount;
            current.price = item.price;
            current.imageUrl = item.imageUrl;
            current.CategoryId = item.CategoryId;
            if (current.CreatedAt == default)
            {
                current.CreatedAt = item.CreatedAt == default ? DateTime.UtcNow : item.CreatedAt;
            }

            if (current.Variants.Count == 0)
            {
                current.stock_quantity = item.stock_quantity;
                ItemInventoryData.EnsureVariants(current);
            }
            else if (current.Variants.Count == 1)
            {
                current.Variants[0].StockQuantity = Math.Clamp(item.stock_quantity, 0, InputLimits.StockMax);
                ItemInventoryData.SyncStock(current);
            }
            else
            {
                ItemInventoryData.SyncStock(current);
            }

            await _context.SaveChangesAsync();
            await _alerts.NotifyRestockAsync(item.id);
        }

        public async Task DeleteItemAsync(int id)
        {
            var item = await _context.Items.Include(i => i.Variants).FirstOrDefaultAsync(i => i.id == id);
            if (item != null)
            {
                var alerts = await _context.StockAlerts.Where(alert => alert.ItemId == id).ToListAsync();
                _context.StockAlerts.RemoveRange(alerts);
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                                 .OrderBy(c => c.Name)
                                 .ToListAsync();
        }

        public bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.id == id);
        }

        private static IQueryable<Item> ApplyCategoryFilter(IQueryable<Item> itemsQuery, string? category)
        {
            if (string.IsNullOrWhiteSpace(category) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                return itemsQuery;
            }

            return itemsQuery.Where(i =>
                i.category != null &&
                (i.category.Name == category || (i.category.Parent != null && i.category.Parent.Name == category)));
        }

        private static IQueryable<Item> ApplySearchFilter(IQueryable<Item> itemsQuery, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return itemsQuery;
            }

            var term = searchTerm.Trim().ToLower();
            return itemsQuery.Where(i =>
                (i.name != null && i.name.ToLower().Contains(term)) ||
                (i.description != null && i.description.ToLower().Contains(term)) ||
                (i.Brand != null && i.Brand.ToLower().Contains(term)) ||
                (i.Features != null && i.Features.ToLower().Contains(term)));
        }

        private async Task<List<Item>> SortItemsAsync(List<Item> items, string? sort)
        {
            var key = (sort ?? "newest").Trim().ToLowerInvariant();
            if (key == "price-asc")
            {
                return items.OrderBy(i => i.DiscountedPrice).ThenBy(i => i.name).ToList();
            }

            if (key == "price-desc")
            {
                return items.OrderByDescending(i => i.DiscountedPrice).ThenBy(i => i.name).ToList();
            }

            if (key == "popular")
            {
                var sales = await _context.OrderLines
                    .GroupBy(line => line.ItemId)
                    .Select(group => new { ItemId = group.Key, Qty = group.Sum(x => x.Quantity) })
                    .ToListAsync();
                var sold = sales.ToDictionary(x => x.ItemId, x => x.Qty);
                return items
                    .OrderByDescending(i => sold.GetValueOrDefault(i.id))
                    .ThenByDescending(i => i.CreatedAt)
                    .ThenBy(i => i.name)
                    .ToList();
            }

            return items.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.id).ToList();
        }
    }
}
