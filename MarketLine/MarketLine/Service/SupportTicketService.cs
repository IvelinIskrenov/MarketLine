using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Security;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly MarketLineDbContext _context;
        private readonly IOrderService _orders;

        public SupportTicketService(MarketLineDbContext context, IOrderService orders)
        {
            _context = context;
            _orders = orders;
        }

        public async Task<(SupportTicket? Ticket, string? Error)> CreateAsync(
            string userId,
            string? subject,
            string? topic,
            string? message,
            string? orderNumber,
            string customerName,
            string email)
        {
            subject = Clean(subject, InputLimits.TicketSubject);
            if (subject == null)
            {
                return (null, "Въведете тема.");
            }

            message = Clean(message, InputLimits.TicketMessage);
            if (message == null)
            {
                return (null, "Въведете описание на проблема.");
            }

            orderNumber = Clean(orderNumber, InputLimits.OrderNumber);
            if (orderNumber != null)
            {
                var order = await _orders.GetByNumberAsync(orderNumber, userId);
                if (order == null)
                {
                    return (null, "Поръчката не е намерена.");
                }

                orderNumber = order.OrderNumber;
            }

            var name = Clean(customerName, InputLimits.LongName) ?? "Клиент";
            var ticket = new SupportTicket
            {
                TicketNumber = await NextNumberAsync(),
                UserId = userId,
                CustomerName = name,
                Email = Clean(email, InputLimits.Email) ?? string.Empty,
                Subject = subject,
                Topic = TicketTopics.Normalize(topic),
                Status = TicketStatuses.Open,
                OrderNumber = orderNumber,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Messages =
                {
                    new SupportMessage
                    {
                        UserId = userId,
                        AuthorName = name,
                        Body = message,
                        IsFromAdmin = false,
                        CreatedAt = DateTime.Now
                    }
                }
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();
            return (ticket, null);
        }

        public async Task<string?> ReplyAsync(int ticketId, string? message, string userId, string authorName, bool isAdmin)
        {
            var ticket = await LoadAsync(ticketId, isAdmin ? null : userId);
            if (ticket == null)
            {
                return "Билетът не е намерен.";
            }

            if (ticket.IsClosed && !isAdmin)
            {
                return "Билетът е затворен.";
            }

            message = Clean(message, InputLimits.TicketMessage);
            if (message == null)
            {
                return "Въведете съобщение.";
            }

            ticket.Messages.Add(new SupportMessage
            {
                UserId = userId,
                AuthorName = Clean(authorName, InputLimits.LongName) ?? (isAdmin ? "Администратор" : "Клиент"),
                Body = message,
                IsFromAdmin = isAdmin,
                CreatedAt = DateTime.Now
            });
            ticket.UpdatedAt = DateTime.Now;
            ticket.Status = isAdmin ? TicketStatuses.Waiting : TicketStatuses.Open;
            ticket.ClosedAt = null;
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> CloseAsync(int ticketId, string userId, bool isAdmin)
        {
            var ticket = await LoadAsync(ticketId, isAdmin ? null : userId);
            if (ticket == null)
            {
                return "Билетът не е намерен.";
            }

            ticket.Status = TicketStatuses.Closed;
            ticket.ClosedAt = DateTime.Now;
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> SetStatusAsync(int ticketId, string? status)
        {
            var ticket = await LoadAsync(ticketId, null);
            if (ticket == null)
            {
                return "Билетът не е намерен.";
            }

            ticket.Status = TicketStatuses.Normalize(status);
            ticket.ClosedAt = ticket.IsClosed ? DateTime.Now : null;
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return null;
        }

        public Task<List<SupportTicket>> GetByUserAsync(string userId)
        {
            return _context.SupportTickets
                .Where(ticket => ticket.UserId == userId)
                .Include(ticket => ticket.Messages)
                .OrderBy(ticket => ticket.Status == TicketStatuses.Closed)
                .ThenByDescending(ticket => ticket.UpdatedAt)
                .ToListAsync();
        }

        public Task<SupportTicket?> GetByIdAsync(int id, string? userId = null)
        {
            return LoadAsync(id, userId);
        }

        public Task<List<SupportTicket>> GetRecentAsync()
        {
            return _context.SupportTickets
                .Include(ticket => ticket.Messages)
                .OrderBy(ticket => ticket.Status == TicketStatuses.Closed)
                .ThenByDescending(ticket => ticket.UpdatedAt)
                .Take(200)
                .ToListAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var ticket = await _context.SupportTickets
                .Include(current => current.Messages)
                .FirstOrDefaultAsync(current => current.Id == id);
            if (ticket != null)
            {
                _context.SupportTickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }
        }

        private Task<SupportTicket?> LoadAsync(int id, string? userId)
        {
            var query = _context.SupportTickets
                .Include(ticket => ticket.Messages)
                .Where(ticket => ticket.Id == id);
            if (userId != null)
            {
                query = query.Where(ticket => ticket.UserId == userId);
            }

            return query.FirstOrDefaultAsync();
        }

        private async Task<string> NextNumberAsync()
        {
            var prefix = $"ST-{DateTime.Now:yyyyMMdd}-";
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var number = prefix + Random.Shared.Next(1000, 9999);
                if (!await _context.SupportTickets.AnyAsync(ticket => ticket.TicketNumber == number))
                {
                    return number;
                }
            }

            return prefix + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        }

        private static string? Clean(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            return value.Length > max ? value[..max] : value;
        }
    }
}
