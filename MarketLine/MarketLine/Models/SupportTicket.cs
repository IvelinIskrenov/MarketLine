using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MarketLine.Security;

namespace MarketLine.Models
{
    public static class TicketStatuses
    {
        public const string Open = "Отворен";
        public const string Processing = "В обработка";
        public const string Waiting = "Изчаква отговор";
        public const string Closed = "Затворен";

        public static readonly string[] All = { Open, Processing, Waiting, Closed };

        public static string Normalize(string? status)
        {
            return All.Contains(status) ? status! : Open;
        }

        public static bool IsClosed(string? status) => Normalize(status) == Closed;

        public static string BadgeClass(string? status)
        {
            return Normalize(status) switch
            {
                Closed => "bg-secondary",
                Waiting => "bg-info text-dark",
                Processing => "bg-primary",
                _ => "bg-warning text-dark"
            };
        }
    }

    public static class TicketTopics
    {
        public const string Order = "Поръчка";
        public const string Product = "Продукт";
        public const string Delivery = "Доставка";
        public const string Payment = "Плащане";
        public const string Other = "Друго";

        public static readonly string[] All = { Order, Product, Delivery, Payment, Other };

        public static string Normalize(string? topic)
        {
            return All.Contains(topic) ? topic! : Other;
        }
    }

    public class SupportTicket
    {
        public int Id { get; set; }

        [Required]
        [StringLength(InputLimits.OrderNumber)]
        public string TicketNumber { get; set; } = string.Empty;

        public string? UserId { get; set; }

        [Required]
        [StringLength(InputLimits.LongName)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(InputLimits.Email)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(InputLimits.TicketSubject)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(InputLimits.TicketTopic)]
        public string Topic { get; set; } = TicketTopics.Other;

        [Required]
        [StringLength(InputLimits.TicketTopic)]
        public string Status { get; set; } = TicketStatuses.Open;

        [StringLength(InputLimits.OrderNumber)]
        public string? OrderNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public DateTime? ClosedAt { get; set; }

        public List<SupportMessage> Messages { get; set; } = new();

        [NotMapped]
        public bool IsClosed => TicketStatuses.IsClosed(Status);
    }

    public class SupportMessage
    {
        public int Id { get; set; }

        public int TicketId { get; set; }

        public SupportTicket? Ticket { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(InputLimits.LongName)]
        public string AuthorName { get; set; } = string.Empty;

        [Required]
        [StringLength(InputLimits.TicketMessage)]
        public string Body { get; set; } = string.Empty;

        public bool IsFromAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
