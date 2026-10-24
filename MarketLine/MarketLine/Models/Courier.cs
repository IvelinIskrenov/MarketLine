using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class Courier
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Куриер")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Цена за доставка")]
        public decimal Price { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;
    }

    public class ShippingSettings
    {
        public int Id { get; set; }

        [Display(Name = "Стандартна цена")]
        public decimal DefaultPrice { get; set; } = 5.99m;

        [Display(Name = "Безплатна доставка над")]
        public decimal FreeOver { get; set; } = 100m;
    }
}
