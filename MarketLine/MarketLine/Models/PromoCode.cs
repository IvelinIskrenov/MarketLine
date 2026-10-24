using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class PromoCode
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Код")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Процентна отстъпка")]
        public bool IsPercent { get; set; }

        [Display(Name = "Стойност")]
        public decimal Value { get; set; }

        [Display(Name = "Минимална сума")]
        public decimal MinSubTotal { get; set; }

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Начало")]
        public DateTime? StartsAt { get; set; }

        [Display(Name = "Край")]
        public DateTime? EndsAt { get; set; }
    }
}
