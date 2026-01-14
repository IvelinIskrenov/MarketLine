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
            return await _context.Items.ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetFilteredItemsAsync(string? category)
        {
            var itemsQuery = _context.Items.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                itemsQuery = itemsQuery.Where(i => i.category == category);
            }

            return await itemsQuery.ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items.FirstOrDefaultAsync(m => m.id == id);
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

        public bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.id == id);
        }
    }
}