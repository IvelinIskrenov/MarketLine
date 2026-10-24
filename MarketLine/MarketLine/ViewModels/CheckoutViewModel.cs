using System.ComponentModel.DataAnnotations;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class CheckoutForm
    {
        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(InputLimits.LongName)]
        [Display(Name = "Име")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилията е задължителна.")]
        [StringLength(InputLimits.LongName)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Въведете валиден имейл.")]
        [StringLength(InputLimits.Email)]
        [Display(Name = "Имейл")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефонът е задължителен.")]
        [StringLength(InputLimits.Phone)]
        [RegularExpression(InputLimits.PhonePattern, ErrorMessage = "Въведете валиден телефон.")]
        [Display(Name = "Телефон")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Адресът е задължителен.")]
        [StringLength(InputLimits.Address)]
        [Display(Name = "Адрес")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Градът е задължителен.")]
        [StringLength(InputLimits.City)]
        [Display(Name = "Град")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пощенският код е задължителен.")]
        [StringLength(InputLimits.PostalCode)]
        [RegularExpression(InputLimits.PostalPattern, ErrorMessage = "Въведете валиден пощенски код.")]
        [Display(Name = "Пощенски код")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Изберете куриер.")]
        [StringLength(InputLimits.LongName)]
        [Display(Name = "Куриер")]
        public string Courier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Изберете начин на доставка.")]
        [StringLength(InputLimits.LongName)]
        [Display(Name = "Начин на доставка")]
        public string DeliveryMethod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Изберете метод за плащане.")]
        [StringLength(InputLimits.LongName)]
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
