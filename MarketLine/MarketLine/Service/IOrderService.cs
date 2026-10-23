using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IOrderService
    {
        Task<Order> PlaceAsync(CheckoutForm form, CartViewModel cart, string? userId);
        Task<Order?> GetByNumberAsync(string orderNumber, string? userId = null);
        Task<List<Order>> GetByUserAsync(string userId);
    }
}
