using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IWishlistService
    {
        Task<HashSet<int>> GetItemIdsAsync(string userId);
        Task<List<Item>> GetItemsAsync(string userId);
        Task<bool> ToggleAsync(string userId, int itemId);
    }
}
