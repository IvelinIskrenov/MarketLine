using MarketLine.Models;

namespace MarketLine.Services
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync();
        Task<string?> AddAsync(int itemId, int quantity = 1);
        Task RemoveAsync(int itemId);
        Task<string?> UpdateQuantityAsync(int itemId, int quantity);
        Task<string?> ApplyPromoAsync(string? code);
        void RemovePromo();
        void Clear();
        int GetCount();
    }
}
