using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IItemService
    {
        Task<IEnumerable<Item>> GetAllItemsAsync();
        Task<IEnumerable<Item>> GetFilteredItemsAsync(string? category);
        Task<IEnumerable<Item>> GetFilteredItemsAsync(string? category, string? searchTerm);
        Task<Item?> GetByIdAsync(int id);
        Task CreateItemAsync(Item item);
        Task UpdateItemAsync(Item item);
        Task DeleteItemAsync(int id);
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        bool ItemExists(int id);
    }
}
