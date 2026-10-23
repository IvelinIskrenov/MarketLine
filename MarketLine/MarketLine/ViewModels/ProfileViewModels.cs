using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class ProfileForm
    {
        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(20)]
        [Display(Name = "Име")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилията е задължителна.")]
        [StringLength(20)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Въведете валиден имейл.")]
        [Display(Name = "Имейл")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Телефон")]
        public string? Phone { get; set; }
    }

    public class AddressForm
    {
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

        [Display(Name = "Адрес по подразбиране")]
        public bool IsDefault { get; set; }
    }

    public class AddressesPageViewModel
    {
        public List<Address> Addresses { get; set; } = new();
        public AddressForm NewAddress { get; set; } = new();
    }
}
