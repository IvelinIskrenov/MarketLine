using MarketLine.Models;
using MarketLine.Security;
using MarketLine.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IShopAdminService _admin;
        private readonly IOrderService _orders;
        private readonly IItemService _items;

        public AdminController(IShopAdminService admin, IOrderService orders, IItemService items)
        {
            _admin = admin;
            _orders = orders;
            _items = items;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "Dashboard";
            return View(await _admin.GetStatsAsync());
        }

        [HttpGet]
        public IActionResult Products()
        {
            return RedirectToAction("AdminControl", "Item");
        }

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            ViewBag.AdminSection = "Categories";
            return View(await _admin.GetCategoriesAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, int? parentId)
        {
            if (!string.IsNullOrWhiteSpace(name) && name.Length <= InputLimits.Category)
            {
                await _admin.AddCategoryAsync(name, parentId);
            }

            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _admin.DeleteCategoryAsync(id);
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            ViewBag.AdminSection = "Orders";
            return View(await _orders.GetAllAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Order(int id)
        {
            ViewBag.AdminSection = "Orders";
            var order = await _orders.GetByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetOrderStatus(int id, string status)
        {
            await _orders.SetStatusAsync(id, status);
            return RedirectToAction(nameof(Order), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            await _orders.CancelAsync(id);
            return RedirectToAction(nameof(Order), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            ViewBag.AdminSection = "Inventory";
            return View(await _items.GetAllItemsAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int itemId, int quantity)
        {
            if (itemId > 0 && quantity >= 0 && quantity <= InputLimits.StockMax)
            {
                await _admin.UpdateStockAsync(itemId, quantity);
            }

            return RedirectToAction(nameof(Inventory));
        }

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            ViewBag.AdminSection = "Customers";
            return View(await _admin.GetCustomersAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Customer(string id)
        {
            ViewBag.AdminSection = "Customers";
            var customer = await _admin.GetCustomerAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            ViewBag.AdminSection = "Promotions";
            return View(await _admin.GetPromosAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPromo(PromoCode promo)
        {
            if (!string.IsNullOrWhiteSpace(promo.Code))
            {
                await _admin.AddPromoAsync(promo);
            }

            return RedirectToAction(nameof(Promotions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromo(int id)
        {
            await _admin.TogglePromoAsync(id);
            return RedirectToAction(nameof(Promotions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromo(int id)
        {
            await _admin.DeletePromoAsync(id);
            return RedirectToAction(nameof(Promotions));
        }

        [HttpGet]
        public async Task<IActionResult> Shipping()
        {
            ViewBag.AdminSection = "Shipping";
            ViewBag.Couriers = await _admin.GetCouriersAsync();
            return View(await _admin.GetShippingAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveShipping(ShippingSettings settings)
        {
            await _admin.SaveShippingAsync(settings);
            return RedirectToAction(nameof(Shipping));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCourier(Courier courier)
        {
            if (!string.IsNullOrWhiteSpace(courier.Name))
            {
                await _admin.AddCourierAsync(courier);
            }

            return RedirectToAction(nameof(Shipping));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourier(int id)
        {
            await _admin.ToggleCourierAsync(id);
            return RedirectToAction(nameof(Shipping));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourier(int id)
        {
            await _admin.DeleteCourierAsync(id);
            return RedirectToAction(nameof(Shipping));
        }

        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            ViewBag.AdminSection = "Payments";
            return View(await _orders.GetAllAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPaymentStatus(int id, string paymentStatus, string? returnAction)
        {
            await _orders.SetPaymentStatusAsync(id, paymentStatus);
            if (string.Equals(returnAction, "Order", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Order), new { id });
            }

            return RedirectToAction(nameof(Payments));
        }

        [HttpGet]
        public async Task<IActionResult> Content()
        {
            ViewBag.AdminSection = "Content";
            return View(await _admin.GetPagesAsync());
        }

        [HttpGet]
        public async Task<IActionResult> EditPage(string key)
        {
            ViewBag.AdminSection = "Content";
            var page = await _admin.GetPageAsync(key);
            if (page == null) return NotFound();
            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPage(SitePage page)
        {
            await _admin.SavePageAsync(page);
            return RedirectToAction(nameof(Content));
        }
    }
}
