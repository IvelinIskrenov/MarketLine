using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLine.Models
{
    public class Item
    {
        [Key]
        public int id { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(100, ErrorMessage = "Името не може да е по-дълго от 100 символа.")]
        [Display(Name = "Име на продукт")]
        public string? name { get; set; }

        [Display(Name = "Описание")]
        public string? description { get; set; }

        [Range(0, 100, ErrorMessage = "Отстъпката трябва да е между 0 и 100%.")]
        [Display(Name = "Отстъпка (%)")]
        public double discount { get; set; }

        [Required(ErrorMessage = "Цената е задължителна.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цената трябва да е над 0.")]
        [Display(Name = "Цена")]
        public decimal price { get; set; }

        [Display(Name = "URL на изображение")]
        public string? imageUrl { get; set; }

        [Required(ErrorMessage = "Категорията е задължителна.")]
        [Display(Name = "Категория")]
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]

        public Category? category { get; set; }

        [Required(ErrorMessage = "Наличността е задължителна.")]
        [Range(0, int.MaxValue, ErrorMessage = "Наличността не може да е отрицателна.")]
        [Display(Name = "Налично количество")]
        public int stock_quantity { get; set; }

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
