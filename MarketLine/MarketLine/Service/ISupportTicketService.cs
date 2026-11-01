using MarketLine.Models;

namespace MarketLine.Services
{
    public interface ISupportTicketService
    {
        Task<(SupportTicket? Ticket, string? Error)> CreateAsync(
            string userId,
            string? subject,
            string? topic,
            string? message,
            string? orderNumber,
            string customerName,
            string email);

        Task<string?> ReplyAsync(int ticketId, string? message, string userId, string authorName, bool isAdmin);
        Task<string?> CloseAsync(int ticketId, string userId, bool isAdmin);
        Task<string?> SetStatusAsync(int ticketId, string? status);
        Task<List<SupportTicket>> GetByUserAsync(string userId);
        Task<SupportTicket?> GetByIdAsync(int id, string? userId = null);
        Task<List<SupportTicket>> GetRecentAsync();
        Task DeleteAsync(int id);
    }
}
