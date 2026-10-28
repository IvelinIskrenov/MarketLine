using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MarketLine.Security;

namespace MarketLine.Models
{
    public static class InventoryRules
    {
        public const int LowStockThreshold = 5;
        public const string DefaultLabel = "Стандартен";
    }

    public class ItemVariant
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        public Item? Item { get; set; }

        [StringLength(InputLimits.VariantLabel)]
        [Display(Name = "Размер")]
        public string? Size { get; set; }

        [StringLength(InputLimits.VariantLabel)]
        [Display(Name = "Цвят")]
        public string? Color { get; set; }

        [StringLength(InputLimits.VariantLabel)]
        [Display(Name = "Модел")]
        public string? Model { get; set; }

        [Range(0, InputLimits.StockMax)]
        [Display(Name = "Наличност")]
        public int StockQuantity { get; set; }

        [NotMapped]
        public string Label
        {
            get
            {
                var parts = new[] { Size, Color, Model }
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Select(part => part!.Trim())
                    .ToArray();
                return parts.Length == 0 ? InventoryRules.DefaultLabel : string.Join(" · ", parts);
            }
        }

        [NotMapped]
        public bool IsSoldOut => StockQuantity <= 0;

        [NotMapped]
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= InventoryRules.LowStockThreshold;
    }
}
