using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(20)]
        public string LastName { get; set; }

        public bool IsAdmin { get; set; } = false;

        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
