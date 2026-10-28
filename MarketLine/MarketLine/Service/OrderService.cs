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

        public async Task<(Order? Order, string? Error)> PlaceAsync(CheckoutForm form, CartViewModel cart, string? userId)
        {
            if (cart.Lines.Count == 0)
            {
                return (null, "Количката е празна.");
            }

            var itemIds = cart.Lines.Select(line => line.ItemId).Distinct().ToList();
            var items = await _context.Items
                .Include(item => item.Variants)
                .Where(item => itemIds.Contains(item.id))
                .ToListAsync();

            foreach (var line in cart.Lines)
            {
                var item = items.FirstOrDefault(candidate => candidate.id == line.ItemId);
                if (item == null)
                {
                    return (null, "Продуктът не е намерен.");
                }

                var variant = ItemInventoryData.Resolve(item, line.VariantId > 0 ? line.VariantId : null);
                var available = variant?.StockQuantity ?? item.stock_quantity;
                if (available < line.Quantity)
                {
                    return (null, $"{line.Name} е изчерпан или няма достатъчно количество.");
                }
            }

            var order = new Order
            {
                OrderNumber = $"ML-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                UserId = userId,
                CreatedAt = DateTime.Now,
                Status = OrderStatuses.New,
                PaymentStatus = PaymentStatuses.Pending,
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
                    VariantId = line.VariantId > 0 ? line.VariantId : null,
                    VariantLabel = line.VariantLabel,
                    Name = line.Name ?? "Продукт",
                    ImageUrl = line.ImageUrl,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity
                }).ToList()
            };

            foreach (var line in cart.Lines)
            {
                var item = items.First(candidate => candidate.id == line.ItemId);
                var variant = ItemInventoryData.Resolve(item, line.VariantId > 0 ? line.VariantId : null);
                if (variant != null)
                {
                    variant.StockQuantity -= line.Quantity;
                    ItemInventoryData.SyncStock(item);
                }
                else
                {
                    item.stock_quantity -= line.Quantity;
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return (order, null);
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            var order = await _context.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return null;
            await RefreshStatusAsync(order);
            return order;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Lines)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            foreach (var order in orders)
            {
                await RefreshStatusAsync(order);
            }

            return orders;
        }

        public async Task SetStatusAsync(int id, string status)
        {
            status = OrderStatuses.Normalize(status);
            if (!OrderStatuses.All.Contains(status)) return;

            var order = await _context.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return;

            await ApplyStatusAsync(order, status, managedByAdmin: true);
        }

        public async Task CancelAsync(int id)
        {
            var order = await _context.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return;

            await ApplyStatusAsync(order, OrderStatuses.Cancelled, managedByAdmin: true);
        }

        public async Task SetPaymentStatusAsync(int id, string paymentStatus)
        {
            if (!PaymentStatuses.All.Contains(paymentStatus)) return;

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return;

            order.PaymentStatus = paymentStatus;
            await _context.SaveChangesAsync();
        }

        public async Task<Order?> GetByNumberAsync(string orderNumber, string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.Lines)
                .Where(o => o.OrderNumber == orderNumber);

            query = userId == null
                ? query.Where(o => o.UserId == null)
                : query.Where(o => o.UserId == userId);

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
            var normalized = OrderStatuses.Normalize(order.Status);
            if (order.StatusManagedByAdmin || normalized == OrderStatuses.Cancelled)
            {
                if (order.Status != normalized)
                {
                    order.Status = normalized;
                    await _context.SaveChangesAsync();
                }

                return;
            }

            var nextStatus = OrderStatuses.FromAge(order.CreatedAt);
            if (OrderStatuses.IndexOf(nextStatus) <= OrderStatuses.IndexOf(normalized)
                && order.Status == normalized)
            {
                return;
            }

            if (OrderStatuses.IndexOf(nextStatus) > OrderStatuses.IndexOf(normalized))
            {
                normalized = nextStatus;
            }

            if (order.Status == normalized)
            {
                return;
            }

            order.Status = normalized;
            await _context.SaveChangesAsync();
        }

        private async Task ApplyStatusAsync(Order order, string status, bool managedByAdmin)
        {
            var previous = OrderStatuses.Normalize(order.Status);
            if (status == OrderStatuses.Cancelled && previous != OrderStatuses.Cancelled)
            {
                foreach (var line in order.Lines)
                {
                    await RestoreStockAsync(line);
                }
            }

            order.Status = status;
            if (managedByAdmin)
            {
                order.StatusManagedByAdmin = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task RestoreStockAsync(OrderLine line)
        {
            if (line.VariantId is int variantId)
            {
                var variant = await _context.ItemVariants
                    .Include(current => current.Item)
                    .ThenInclude(item => item!.Variants)
                    .FirstOrDefaultAsync(current => current.Id == variantId);
                if (variant?.Item == null)
                {
                    return;
                }

                variant.StockQuantity += line.Quantity;
                ItemInventoryData.SyncStock(variant.Item);
                return;
            }

            var item = await _context.Items
                .Include(current => current.Variants)
                .FirstOrDefaultAsync(current => current.id == line.ItemId);
            if (item == null)
            {
                return;
            }

            if (item.Variants.Count == 1)
            {
                item.Variants[0].StockQuantity += line.Quantity;
                ItemInventoryData.SyncStock(item);
                return;
            }

            item.stock_quantity += line.Quantity;
        }
    }
}
