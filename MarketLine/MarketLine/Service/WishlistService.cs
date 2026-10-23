using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly MarketLineDbContext _context;

        public WishlistService(MarketLineDbContext context)
        {
            _context = context;
        }

        public async Task<HashSet<int>> GetItemIdsAsync(string userId)
        {
            var ids = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.ItemId)
                .ToListAsync();

            return ids.ToHashSet();
        }

        public Task<List<Item>> GetItemsAsync(string userId)
        {
            return _context.WishlistItems
                .Where(w => w.UserId == userId && w.Item != null)
                .Include(w => w.Item)
                    .ThenInclude(i => i!.category)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => w.Item!)
                .ToListAsync();
        }

        public async Task<bool> ToggleAsync(string userId, int itemId)
        {
            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ItemId == itemId);

            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                await _context.SaveChangesAsync();
                return false;
            }

            var itemExists = await _context.Items.AnyAsync(i => i.id == itemId);
            if (!itemExists) return false;

            _context.WishlistItems.Add(new WishlistItem
            {
                UserId = userId,
                ItemId = itemId,
                AddedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
