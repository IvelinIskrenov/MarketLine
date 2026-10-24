using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class Courier
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Куриер")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 1000)]
        [Display(Name = "Цена за доставка")]
        public decimal Price { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;
    }

    public class ShippingSettings
    {
        public int Id { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Стандартна цена")]
        public decimal DefaultPrice { get; set; } = 5.99m;

        [Range(0, 100000)]
        [Display(Name = "Безплатна доставка над")]
        public decimal FreeOver { get; set; } = 100m;
    }
}
