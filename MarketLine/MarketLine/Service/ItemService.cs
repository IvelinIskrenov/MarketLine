using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class ItemService : IItemService
    {
        private readonly MarketLineDbContext _context;

        public ItemService(MarketLineDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            return await _context.Items.Include(i => i.category).ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetFilteredItemsAsync(string? category)
        {
            var itemsQuery = _context.Items
                .Include(i => i.category)
                .ThenInclude(c => c!.Parent)
                .AsQueryable();

            itemsQuery = ApplyCategoryFilter(itemsQuery, category);

            return await itemsQuery.ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetFilteredItemsAsync(string? category, string? searchTerm)
        {
            var itemsQuery = _context.Items
                .Include(i => i.category)
                .ThenInclude(c => c!.Parent)
                .AsQueryable();

            itemsQuery = ApplyCategoryFilter(itemsQuery, category);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                itemsQuery = itemsQuery.Where(i => i.name.Contains(searchTerm));
            }

            return await itemsQuery.ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items.Include(i => i.category).FirstOrDefaultAsync(m => m.id == id);
        }

        public async Task CreateItemAsync(Item item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateItemAsync(Item item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteItemAsync(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
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
    }
}