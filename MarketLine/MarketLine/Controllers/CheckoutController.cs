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
        private readonly IShopAdminService _admin;
        private readonly UserManager<User> _userManager;

        public CheckoutController(
            ICartService cartService,
            ICheckoutService checkoutService,
            IOrderService orderService,
            IAddressService addressService,
            IShopAdminService admin,
            UserManager<User> userManager)
        {
            _cartService = cartService;
            _checkoutService = checkoutService;
            _orderService = orderService;
            _addressService = addressService;
            _admin = admin;
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
            await FillCouriersAsync();
            return View(form);
        }

        /// <summary>
        /// Saves the checkout data and opens the order review page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("FirstName,LastName,Email,Phone,Address,City,PostalCode,Courier,DeliveryMethod,PaymentMethod")] CheckoutForm model)
        {
            var cart = await _cartService.GetCartAsync();
            if (cart.Lines.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                await FillCouriersAsync();
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

            await ApplyCourierShippingAsync(cart, draft.Courier);
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
            await ApplyCourierShippingAsync(cart, draft.Courier);
            var order = await _orderService.PlaceAsync(draft, cart, userId);
            _checkoutService.ClearDraft();
            _cartService.Clear();
            HttpContext.Session.SetString("LastOrderNumber", order.OrderNumber);
            return RedirectToAction(nameof(Success), new { orderNumber = order.OrderNumber });
        }

        /// <summary>
        /// Shows the successful order confirmation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Success(string orderNumber)
        {
            var order = await FindAccessibleOrderAsync(orderNumber);
            if (order == null)
            {
                return RedirectToAction("Index", "Item");
            }

            return View(order);
        }

        private async Task<Order?> FindAccessibleOrderAsync(string? orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length > 40)
            {
                return null;
            }

            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                return await _orderService.GetByNumberAsync(orderNumber, userId);
            }

            var lastOrder = HttpContext.Session.GetString("LastOrderNumber");
            if (!string.Equals(lastOrder, orderNumber, StringComparison.Ordinal))
            {
                return null;
            }

            return await _orderService.GetByNumberAsync(orderNumber);
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

        private async Task FillCouriersAsync()
        {
            ViewBag.Couriers = await _admin.GetCouriersAsync(true);
        }

        private async Task ApplyCourierShippingAsync(CartViewModel cart, string courierName)
        {
            var settings = await _admin.GetShippingAsync();
            var courier = (await _admin.GetCouriersAsync(true))
                .FirstOrDefault(c => c.Name == courierName);
            var price = courier?.Price ?? settings.DefaultPrice;
            cart.Shipping = cart.SubTotal >= settings.FreeOver ? 0m : price;
            cart.Total = Math.Max(0, cart.SubTotal - cart.PromoDiscount + cart.Shipping);
        }
    }
}
