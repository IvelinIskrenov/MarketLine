using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class CheckoutForm
    {
        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(50)]
        [Display(Name = "Име")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилията е задължителна.")]
        [StringLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Въведете валиден имейл.")]
        [Display(Name = "Имейл")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефонът е задължителен.")]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Адресът е задължителен.")]
        [Display(Name = "Адрес")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Градът е задължителен.")]
        [Display(Name = "Град")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пощенският код е задължителен.")]
        [Display(Name = "Пощенски код")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Изберете куриер.")]
        [Display(Name = "Куриер")]
        public string Courier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Изберете начин на доставка.")]
        [Display(Name = "Начин на доставка")]
        public string DeliveryMethod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Изберете метод за плащане.")]
        [Display(Name = "Метод за плащане")]
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class CheckoutReviewViewModel
    {
        public CheckoutForm Customer { get; set; } = new();
        public CartViewModel Cart { get; set; } = new();
    }

    public class PlacedOrder
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public CheckoutForm Customer { get; set; } = new();
        public List<CartLine> Lines { get; set; } = new();
        public string? PromoCode { get; set; }
        public decimal SubTotal { get; set; }
        public decimal PromoDiscount { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
    }
}
