using System.ComponentModel.DataAnnotations;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(InputLimits.Category)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        public Category? Parent { get; set; }

        public List<Category> Children { get; set; } = new();

        public List<Item>? Items { get; set; }

        public Category() { }
    }
}
