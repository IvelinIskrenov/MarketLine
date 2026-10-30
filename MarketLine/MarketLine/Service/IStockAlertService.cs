using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IStockAlertService
    {
        Task<string?> SubscribeAsync(int itemId, int variantId, string? email, string? userId);
        Task<int> NotifyRestockAsync(int itemId);
        Task<List<StockAlert>> GetRecentAsync();
    }
}
