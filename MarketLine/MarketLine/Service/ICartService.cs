using MarketLine.Models;

namespace MarketLine.Services
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync();
        Task<string?> AddAsync(int itemId, int quantity = 1, int variantId = 0);
        Task RemoveAsync(int itemId, int variantId = 0);
        Task<string?> UpdateQuantityAsync(int itemId, int quantity, int variantId = 0);
        Task<string?> ApplyPromoAsync(string? code);
        void RemovePromo();
        void Clear();
        int GetCount();
    }
}
