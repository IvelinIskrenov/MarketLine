using System.ComponentModel.DataAnnotations;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class Address
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Етикет")]
        public string? Label { get; set; }

        [Required(ErrorMessage = "Адресът е задължителен.")]
        [StringLength(InputLimits.Address)]
        [Display(Name = "Адрес")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Градът е задължителен.")]
        [StringLength(InputLimits.City)]
        [Display(Name = "Град")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пощенският код е задължителен.")]
        [StringLength(InputLimits.PostalCode)]
        [RegularExpression(InputLimits.PostalPattern, ErrorMessage = "Въведете валиден пощенски код.")]
        [Display(Name = "Пощенски код")]
        public string PostalCode { get; set; } = string.Empty;

        public bool IsDefault { get; set; }
    }
}
