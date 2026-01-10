using MarketLine.Data;
using MarketLine.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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

        [HttpGet] // /AdminControl
        public async Task<IActionResult> AdminControl()
        {
            List<Item> item = await _context.Items.ToListAsync();
            return View(item);
        }

        [HttpGet] // /Index
        public async Task<IActionResult> Index(string? category)
        {
            var itemsQuery = _context.Items.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                if (category != "All")
                {
                    itemsQuery = itemsQuery.Where(i => i.category == category);
                }
            }

            List<Item> items = await itemsQuery.ToListAsync();

            ViewData["CurrentCategory"] = category;

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
        [Bind("name,description,discount,price,imageUrl,category,stock_quantity")]
        Item item)
        {
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        [HttpGet] // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FirstOrDefaultAsync(m => m.id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpGet] // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        [HttpPost] // POST: Items/Edit/5
        public async Task<IActionResult> Edit(int id,
        [Bind("id,name,description,discount,price,imageUrl,category,stock_quantity")]
        Item item)
        {
            if (id != item.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Items.Any(e => e.id == item.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index)); 
            }
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AdminControl));
        }
    }
}
