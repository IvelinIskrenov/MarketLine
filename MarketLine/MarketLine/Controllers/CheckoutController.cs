using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ICheckoutService _checkoutService;
        private readonly IOrderService _orderService;
        private readonly IAddressService _addressService;
        private readonly UserManager<User> _userManager;

        public CheckoutController(
            ICartService cartService,
            ICheckoutService checkoutService,
            IOrderService orderService,
            IAddressService addressService,
            UserManager<User> userManager)
        {
            _cartService = cartService;
            _checkoutService = checkoutService;
            _orderService = orderService;
            _addressService = addressService;
            _userManager = userManager;
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

            var form = _checkoutService.GetDraft() ?? await CreatePrefillAsync();
            return View(form);
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

            var userId = _userManager.GetUserId(User);
            var order = await _orderService.PlaceAsync(draft, cart, userId);
            _checkoutService.ClearDraft();
            _cartService.Clear();
            return RedirectToAction(nameof(Success), new { orderNumber = order.OrderNumber });
        }

        /// <summary>
        /// Shows the successful order confirmation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Success(string orderNumber)
        {
            var order = await _orderService.GetByNumberAsync(orderNumber);
            if (order == null)
            {
                return RedirectToAction("Index", "Item");
            }

            return View(order);
        }

        private async Task<CheckoutForm> CreatePrefillAsync()
        {
            var form = new CheckoutForm();
            if (User.Identity?.IsAuthenticated != true)
            {
                return form;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return form;

            form.FirstName = user.FirstName;
            form.LastName = user.LastName;
            form.Email = user.Email ?? string.Empty;
            form.Phone = user.PhoneNumber ?? string.Empty;

            var address = await _addressService.GetDefaultAsync(user.Id);
            if (address != null)
            {
                form.Address = address.AddressLine;
                form.City = address.City;
                form.PostalCode = address.PostalCode;
            }

            return form;
        }
    }
}
