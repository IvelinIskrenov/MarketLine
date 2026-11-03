using MarketLine.Localization;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    public class LanguageController : Controller
    {
        [HttpGet]
        public IActionResult Set(string culture, string? returnUrl)
        {
            ShopCultures.SetCookie(Response, culture);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Item");
        }
    }
}
