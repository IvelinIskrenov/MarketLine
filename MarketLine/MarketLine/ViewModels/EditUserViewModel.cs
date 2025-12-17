using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(20)]
        public string LastName { get; set; }

        public string Email { get; set; }

        public List<string> AllRoles { get; set; }

        public List<string> UserRoles { get; set; }
    }
}