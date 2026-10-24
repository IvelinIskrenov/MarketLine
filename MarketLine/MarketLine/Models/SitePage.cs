using System.ComponentModel.DataAnnotations;

namespace MarketLine.Models
{
    public class SitePage
    {
        public int Id { get; set; }

        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Заглавие")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Текст")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Изображение (URL)")]
        public string? ImageUrl { get; set; }
    }
}
