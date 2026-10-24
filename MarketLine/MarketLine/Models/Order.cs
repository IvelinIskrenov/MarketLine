using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLine.Models
{
    public static class OrderStatuses
    {
        public const string Accepted = "Приета";
        public const string Processing = "Обработва се";
        public const string Shipped = "Изпратена";
        public const string Delivered = "Доставена";
        public const string Cancelled = "Отказана";

        public static readonly string[] Steps =
        {
            Accepted,
            Processing,
            Shipped,
            Delivered
        };

        public static readonly string[] All =
        {
            Accepted,
            Processing,
            Shipped,
            Delivered,
            Cancelled
        };

        public static int IndexOf(string? status)
        {
            var index = Array.IndexOf(Steps, status);
            return index < 0 ? 0 : index;
        }

        public static string FromAge(DateTime createdAt)
        {
            var minutes = (DateTime.Now - createdAt).TotalMinutes;
            if (minutes < 2) return Accepted;
            if (minutes < 8) return Processing;
            if (minutes < 20) return Shipped;
            return Delivered;
        }
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string OrderNumber { get; set; } = string.Empty;

        public string? UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        public string Status { get; set; } = OrderStatuses.Accepted;

        public bool StatusManagedByAdmin { get; set; }

        [Required]
        public string PaymentStatus { get; set; } = PaymentStatuses.Pending;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string Courier { get; set; } = string.Empty;

        [Required]
        public string DeliveryMethod { get; set; } = string.Empty;

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? PromoCode { get; set; }

        public decimal SubTotal { get; set; }

        public decimal PromoDiscount { get; set; }

        public decimal Shipping { get; set; }

        public decimal Total { get; set; }

        public List<OrderLine> Lines { get; set; } = new();
    }

    public static class PaymentStatuses
    {
        public const string Pending = "Очаква плащане";
        public const string Paid = "Платена";
        public const string Refunded = "Възстановена";

        public static readonly string[] All =
        {
            Pending,
            Paid,
            Refunded
        };
    }

    public class OrderLine
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public Order? Order { get; set; }

        public int ItemId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [NotMapped]
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
