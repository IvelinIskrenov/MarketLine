using MarketLine.Data;
using MarketLine.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Controllers
{
    public class ItemController : Controller
    {
        private readonly MarketLineDbContext _context;

        public ItemController(MarketLineDbContext context) 
        { 
            _context = context;
        }

        [HttpGet] // /Index
        public async Task<IActionResult> Index()
        {
            List<Item> item = await _context.Items.ToListAsync();
            return View(item);
        }
    }
}
