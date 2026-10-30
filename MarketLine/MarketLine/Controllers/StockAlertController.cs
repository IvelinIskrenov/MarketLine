using MarketLine.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MarketLine.Models;

namespace MarketLine.Controllers
{
    public class StockAlertController : Controller
    {
        private readonly IStockAlertService _alerts;
        private readonly UserManager<User> _users;

        public StockAlertController(IStockAlertService alerts, UserManager<User> users)
        {
            _alerts = alerts;
            _users = users;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int itemId, int variantId = 0, string? email = null, string? returnUrl = null)
        {
            if (itemId <= 0)
            {
                TempData["CartError"] = "Невалиден продукт.";
                return RedirectToAction("Index", "Item");
            }

            var user = await _users.GetUserAsync(User);
            if (string.IsNullOrWhiteSpace(email))
            {
                email = user?.Email;
            }

            var error = await _alerts.SubscribeAsync(itemId, variantId, email, user?.Id);
            if (error != null)
            {
                TempData["CartError"] = error;
            }
            else
            {
                TempData["CartMessage"] = "Ще ви изпратим имейл, когато продуктът е отново в наличност.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Details", "Item", new { id = itemId });
        }
    }
}
