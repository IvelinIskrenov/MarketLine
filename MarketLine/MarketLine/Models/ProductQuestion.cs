using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class ProductQuestion
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        public Item? Item { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(InputLimits.LongName)]
        public string AuthorName { get; set; } = string.Empty;

        [Required]
        [StringLength(InputLimits.Question)]
        public string Question { get; set; } = string.Empty;

        [StringLength(InputLimits.Answer)]
        public string? Answer { get; set; }

        public DateTime AskedAt { get; set; } = DateTime.Now;

        public DateTime? AnsweredAt { get; set; }

        public string? AnsweredByUserId { get; set; }

        [NotMapped]
        public bool IsAnswered => !string.IsNullOrWhiteSpace(Answer);
    }
}
