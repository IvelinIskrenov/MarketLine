using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ItemId { get; set; }

        public Item? Item { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}
