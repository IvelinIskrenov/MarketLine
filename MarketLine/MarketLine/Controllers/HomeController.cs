using System.Diagnostics;
using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IShopAdminService _admin;

        public HomeController(ILogger<HomeController> logger, IShopAdminService admin)
        {
            _logger = logger;
            _admin = admin;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Page(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key == "home")
            {
                return RedirectToAction(nameof(Index));
            }

            var page = await _admin.GetPageAsync(key);
            if (page == null) return NotFound();
            return View(page);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
