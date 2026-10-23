using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class OrderService : IOrderService
    {
        private readonly MarketLineDbContext _context;

        public OrderService(MarketLineDbContext context)
        {
            _context = context;
        }

        public async Task<Order> PlaceAsync(CheckoutForm form, CartViewModel cart, string? userId)
        {
            var order = new Order
            {
                OrderNumber = $"ML-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                UserId = userId,
                CreatedAt = DateTime.Now,
                Status = OrderStatuses.Accepted,
                FirstName = form.FirstName,
                LastName = form.LastName,
                Email = form.Email,
                Phone = form.Phone,
                Address = form.Address,
                City = form.City,
                PostalCode = form.PostalCode,
                Courier = form.Courier,
                DeliveryMethod = form.DeliveryMethod,
                PaymentMethod = form.PaymentMethod,
                PromoCode = cart.PromoCode,
                SubTotal = cart.SubTotal,
                PromoDiscount = cart.PromoDiscount,
                Shipping = cart.Shipping,
                Total = cart.Total,
                Lines = cart.Lines.Select(line => new OrderLine
                {
                    ItemId = line.ItemId,
                    Name = line.Name ?? "Продукт",
                    ImageUrl = line.ImageUrl,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetByNumberAsync(string orderNumber, string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.Lines)
                .Where(o => o.OrderNumber == orderNumber);

            if (userId != null)
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync();
            if (order == null) return null;

            await RefreshStatusAsync(order);
            return order;
        }

        public async Task<List<Order>> GetByUserAsync(string userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Lines)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RefreshStatusAsync(order);
            }

            return orders;
        }

        private async Task RefreshStatusAsync(Order order)
        {
            var nextStatus = OrderStatuses.FromAge(order.CreatedAt);
            if (OrderStatuses.IndexOf(nextStatus) <= OrderStatuses.IndexOf(order.Status))
            {
                return;
            }

            order.Status = nextStatus;
            await _context.SaveChangesAsync();
        }
    }
}
