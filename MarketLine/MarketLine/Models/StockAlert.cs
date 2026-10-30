using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class StockAlert
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        public Item? Item { get; set; }

        public int? VariantId { get; set; }

        public ItemVariant? Variant { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(InputLimits.Email)]
        public string Email { get; set; } = string.Empty;

        public string? UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? NotifiedAt { get; set; }

        [NotMapped]
        public bool IsPending => NotifiedAt == null;
    }
}
