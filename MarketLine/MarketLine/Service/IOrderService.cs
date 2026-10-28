using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IOrderService
    {
        Task<(Order? Order, string? Error)> PlaceAsync(CheckoutForm form, CartViewModel cart, string? userId);
        Task<Order?> GetByNumberAsync(string orderNumber, string? userId = null);
        Task<Order?> GetByIdAsync(int id);
        Task<List<Order>> GetByUserAsync(string userId);
        Task<List<Order>> GetAllAsync();
        Task SetStatusAsync(int id, string status);
        Task CancelAsync(int id);
        Task SetPaymentStatusAsync(int id, string paymentStatus);
    }
}
