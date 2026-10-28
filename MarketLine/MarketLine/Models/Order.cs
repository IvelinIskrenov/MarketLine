using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLine.Models
{
    public static class OrderStatuses
    {
        public const string New = "Нова";
        public const string Confirmed = "Потвърдена";
        public const string Processing = "Обработва се";
        public const string Shipped = "Изпратена";
        public const string Delivered = "Доставена";
        public const string Cancelled = "Отказана";
        public const string Accepted = Confirmed;
        public const string LegacyAccepted = "Приета";

        public static readonly string[] Steps =
        {
            New,
            Confirmed,
            Processing,
            Shipped,
            Delivered
        };

        public static readonly string[] All =
        {
            New,
            Confirmed,
            Processing,
            Shipped,
            Delivered,
            Cancelled
        };

        public static string Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status) || status == LegacyAccepted)
            {
                return string.IsNullOrWhiteSpace(status) ? New : Confirmed;
            }

            return status;
        }

        public static bool IsKnown(string? status)
        {
            var normalized = Normalize(status);
            return All.Contains(normalized);
        }

        public static int IndexOf(string? status)
        {
            var normalized = Normalize(status);
            if (normalized == Cancelled)
            {
                return -1;
            }

            var index = Array.IndexOf(Steps, normalized);
            return index < 0 ? 0 : index;
        }

        public static string FromAge(DateTime createdAt)
        {
            var minutes = (DateTime.Now - createdAt).TotalMinutes;
            if (minutes < 2) return New;
            if (minutes < 8) return Confirmed;
            if (minutes < 20) return Processing;
            if (minutes < 40) return Shipped;
            return Delivered;
        }

        public static string BadgeClass(string? status)
        {
            return Normalize(status) switch
            {
                Cancelled => "bg-danger",
                Delivered => "bg-success",
                Shipped => "bg-primary",
                Processing => "bg-info text-dark",
                Confirmed => "bg-success",
                _ => "bg-warning text-dark"
            };
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
        public string Status { get; set; } = OrderStatuses.New;

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

        public int? VariantId { get; set; }

        public string? VariantLabel { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [NotMapped]
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
