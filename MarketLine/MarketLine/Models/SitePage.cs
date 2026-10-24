using System.ComponentModel.DataAnnotations;
using MarketLine.Security;

namespace MarketLine.Models
{
    public class SitePage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(InputLimits.PageTitle)]
        [Display(Name = "Заглавие")]
        public string Title { get; set; } = string.Empty;

        [StringLength(InputLimits.PageBody)]
        [Display(Name = "Текст")]
        public string Body { get; set; } = string.Empty;

        [StringLength(InputLimits.Url)]
        [HttpUrl]
        [Display(Name = "Изображение (URL)")]
        public string? ImageUrl { get; set; }
    }
}
