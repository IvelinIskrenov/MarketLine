using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

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

        [JsonIgnore]
        public override string? PasswordHash { get; set; }

        [JsonIgnore]
        public override string? SecurityStamp { get; set; }

        [JsonIgnore]
        public override string? ConcurrencyStamp { get; set; }
    }
}
