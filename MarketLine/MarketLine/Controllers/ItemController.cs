using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    public class ItemController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
