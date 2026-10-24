using System.ComponentModel.DataAnnotations;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class ProfileForm
    {
        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(InputLimits.Name)]
        [Display(Name = "Име")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилията е задължителна.")]
        [StringLength(InputLimits.Name)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Въведете валиден имейл.")]
        [StringLength(InputLimits.Email)]
        [Display(Name = "Имейл")]
        public string Email { get; set; } = string.Empty;

        [StringLength(InputLimits.Phone)]
        [RegularExpression(InputLimits.PhonePattern, ErrorMessage = "Въведете валиден телефон.")]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }
    }

    public class AddressForm
    {
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

        [Display(Name = "Адрес по подразбиране")]
        public bool IsDefault { get; set; }
    }

    public class AddressesPageViewModel
    {
        public List<Address> Addresses { get; set; } = new();
        public AddressForm NewAddress { get; set; } = new();
    }
}
