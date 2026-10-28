using MarketLine.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Shows the current cart with totals, promo code and shipping.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync();
            return View(cart);
        }

        /// <summary>
        /// Adds a product to the cart and redirects to the cart page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int quantity = 1, int variantId = 0, string? returnUrl = null)
        {
            var error = await _cartService.AddAsync(id, quantity, variantId);
            if (error != null)
            {
                TempData["CartError"] = error;
            }
            else
            {
                TempData["CartMessage"] = "Продуктът е добавен в количката.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Updates the quantity of a cart line.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, int quantity, int variantId = 0)
        {
            var error = await _cartService.UpdateQuantityAsync(id, quantity, variantId);
            if (error != null)
            {
                TempData["CartError"] = error;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Removes a product from the cart.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, int variantId = 0)
        {
            await _cartService.RemoveAsync(id, variantId);
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Applies a promotional code to the cart.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPromo(string? promoCode)
        {
            var error = await _cartService.ApplyPromoAsync(promoCode);
            if (error != null)
            {
                TempData["CartError"] = error;
            }
            else
            {
                TempData["CartMessage"] = "Промоционалният код е приложен.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Removes the currently applied promotional code.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePromo()
        {
            _cartService.RemovePromo();
            return RedirectToAction(nameof(Index));
        }
    }
}
