using MarketLine.Models;
using MarketLine.Security;
using MarketLine.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IAddressService _addressService;
        private readonly IOrderService _orderService;
        private readonly IWishlistService _wishlistService;

        public ProfileController(
            UserManager<User> userManager,
            IAddressService addressService,
            IOrderService orderService,
            IWishlistService wishlistService)
        {
            _userManager = userManager;
            _addressService = addressService;
            _orderService = orderService;
            _wishlistService = wishlistService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            ViewBag.ProfileSection = "Profile";
            return View(new ProfileForm
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("FirstName,LastName,Email,Phone")] ProfileForm model)
        {
            ViewBag.ProfileSection = "Profile";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, model.Email);
                var setName = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setEmail.Succeeded || !setName.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Имейлът не може да бъде сменен.");
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Данните не могат да бъдат записани.");
                return View(model);
            }

            TempData["ProfileMessage"] = "Личните данни са записани.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            ViewBag.ProfileSection = "Addresses";
            return View(new AddressesPageViewModel
            {
                Addresses = await _addressService.GetByUserAsync(user.Id)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress([Bind(Prefix = "NewAddress")] AddressForm model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                ViewBag.ProfileSection = "Addresses";
                return View("Addresses", new AddressesPageViewModel
                {
                    Addresses = await _addressService.GetByUserAsync(user.Id),
                    NewAddress = model
                });
            }

            await _addressService.AddAsync(user.Id, model);
            TempData["ProfileMessage"] = "Адресът е добавен.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();
            if (id <= 0) return RedirectToAction(nameof(Addresses));

            await _addressService.SetDefaultAsync(user.Id, id);
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();
            if (id <= 0) return RedirectToAction(nameof(Addresses));

            await _addressService.DeleteAsync(user.Id, id);
            TempData["ProfileMessage"] = "Адресът е премахнат.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            ViewBag.ProfileSection = "Orders";
            var orders = await _orderService.GetByUserAsync(user.Id);
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Order(string orderNumber)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length > InputLimits.OrderNumber)
            {
                return RedirectToAction(nameof(Orders));
            }

            var order = await _orderService.GetByNumberAsync(orderNumber, user.Id);
            if (order == null)
            {
                return RedirectToAction(nameof(Orders));
            }

            ViewBag.ProfileSection = "Orders";
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Wishlist()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            ViewBag.ProfileSection = "Wishlist";
            var items = await _wishlistService.GetItemsAsync(user.Id);
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWishlist(int itemId, string? returnUrl = null)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            if (itemId <= 0)
            {
                return RedirectToAction(nameof(Wishlist));
            }

            await _wishlistService.ToggleAsync(user.Id, itemId);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Wishlist));
        }

        private Task<User?> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(User);
        }
    }
}
