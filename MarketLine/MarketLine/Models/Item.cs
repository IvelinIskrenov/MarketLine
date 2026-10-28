using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class Item
    {
        [Key]
        public int id { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(InputLimits.ProductName, ErrorMessage = "Името не може да е по-дълго от 100 символа.")]
        [Display(Name = "Име на продукт")]
        public string? name { get; set; }

        [StringLength(InputLimits.ProductDescription, ErrorMessage = "Описанието е твърде дълго.")]
        [Display(Name = "Описание")]
        public string? description { get; set; }

        [Range(0, 100, ErrorMessage = "Отстъпката трябва да е между 0 и 100%.")]
        [Display(Name = "Отстъпка (%)")]
        public double discount { get; set; }

        [Required(ErrorMessage = "Цената е задължителна.")]
        [Range(0.01, InputLimits.PriceMax, ErrorMessage = "Цената трябва да е над 0.")]
        [Display(Name = "Цена")]
        public decimal price { get; set; }

        [StringLength(InputLimits.Url)]
        [HttpUrl]
        [Display(Name = "URL на изображение")]
        public string? imageUrl { get; set; }

        [Required(ErrorMessage = "Категорията е задължителна.")]
        [Display(Name = "Категория")]
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]

        public Category? category { get; set; }

        [Required(ErrorMessage = "Наличността е задължителна.")]
        [Range(0, InputLimits.StockMax, ErrorMessage = "Наличността не е валидна.")]
        [Display(Name = "Налично количество")]
        public int stock_quantity { get; set; }

        public List<ItemVariant> Variants { get; set; } = new();

        [StringLength(InputLimits.Brand)]
        [Display(Name = "Марка")]
        public string? Brand { get; set; }

        [StringLength(InputLimits.Features)]
        [Display(Name = "Характеристики")]
        public string? Features { get; set; }

        [Display(Name = "Добавен на")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public IReadOnlyList<string> FeatureTags =>
            string.IsNullOrWhiteSpace(Features)
                ? Array.Empty<string>()
                : Features.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        [NotMapped]
        public int AvailableStock => Variants.Count > 0
            ? Variants.Sum(variant => variant.StockQuantity)
            : stock_quantity;

        [NotMapped]
        public bool IsSoldOut => AvailableStock <= 0;

        [NotMapped]
        public bool IsLowStock => AvailableStock > 0 && AvailableStock <= InventoryRules.LowStockThreshold;

        [NotMapped]
        public bool HasMultipleVariants => Variants.Count > 1;

        [NotMapped] 
        [Display(Name = "Цена с отстъпка")]
        public decimal DiscountedPrice
        {
            get
            {
                if (discount <= 0) return price;
                return price - (price * (decimal)(discount / 100));
            }
        }
        public Item() { }
    }
}
