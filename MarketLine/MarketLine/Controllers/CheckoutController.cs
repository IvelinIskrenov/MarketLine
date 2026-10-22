using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ICheckoutService _checkoutService;

        public CheckoutController(ICartService cartService, ICheckoutService checkoutService)
        {
            _cartService = cartService;
            _checkoutService = checkoutService;
        }

        /// <summary>
        /// Loads the checkout form with customer, address, courier and payment fields.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync();
            if (cart.Lines.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(_checkoutService.GetDraft() ?? new CheckoutForm());
        }

        /// <summary>
        /// Saves the checkout data and opens the order review page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutForm model)
        {
            var cart = await _cartService.GetCartAsync();
            if (cart.Lines.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _checkoutService.SaveDraft(model);
            return RedirectToAction(nameof(Review));
        }

        /// <summary>
        /// Shows the full order before confirmation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Review()
        {
            var cart = await _cartService.GetCartAsync();
            var draft = _checkoutService.GetDraft();
            if (cart.Lines.Count == 0 || draft == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new CheckoutReviewViewModel
            {
                Customer = draft,
                Cart = cart
            });
        }

        /// <summary>
        /// Confirms the order, clears the cart and opens the success page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm()
        {
            var cart = await _cartService.GetCartAsync();
            var draft = _checkoutService.GetDraft();
            if (cart.Lines.Count == 0 || draft == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var order = _checkoutService.PlaceOrder(draft, cart);
            _cartService.Clear();
            return RedirectToAction(nameof(Success), new { orderNumber = order.OrderNumber });
        }

        /// <summary>
        /// Shows the successful order confirmation.
        /// </summary>
        [HttpGet]
        public IActionResult Success(string orderNumber)
        {
            var order = _checkoutService.GetOrder(orderNumber);
            if (order == null)
            {
                return RedirectToAction("Index", "Item");
            }

            return View(order);
        }
    }
}
