using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class Address
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "Етикет")]
        public string? Label { get; set; }

        [Required(ErrorMessage = "Адресът е задължителен.")]
        [Display(Name = "Адрес")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Градът е задължителен.")]
        [Display(Name = "Град")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пощенският код е задължителен.")]
        [Display(Name = "Пощенски код")]
        public string PostalCode { get; set; } = string.Empty;

        public bool IsDefault { get; set; }
    }
}
