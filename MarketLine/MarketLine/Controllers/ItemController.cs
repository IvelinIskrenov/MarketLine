using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Controllers
{
    public class ItemController : Controller
    {
        private readonly IItemService _itemService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet]
        public async Task<IActionResult> AdminControl()
        {
            var items = await _itemService.GetAllItemsAsync();
            return View(items);
        }

        //[HttpGet]
        //public async Task<IActionResult> Index(string? category)
        //{
        //    var items = await _itemService.GetFilteredItemsAsync(category);
        //    ViewData["CurrentCategory"] = category ?? "All";
        //    return View(items);
        //}

        [HttpGet]
        public async Task<IActionResult> Index(string? category, string? searchTerm)
        {
            var items = await _itemService.GetFilteredItemsAsync(category, searchTerm);

            ViewData["CurrentCategory"] = category ?? "All";
            ViewData["CurrentSearch"] = searchTerm;

            return View(items);
        }

        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            //Create method item, add ViewBag.Categories for as long as we can access them

            var categories = await _itemService.GetAllCategoriesAsync(); 
            ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("name,description,discount,price,imageUrl,CategoryId,stock_quantity")] Item item)
        {
            //Create method item, add ViewBag.Categories for as long as we can access them
            if (ModelState.IsValid)
            {
                await _itemService.CreateItemAsync(item);
                return RedirectToAction(nameof(Index));
            }

            var categories = await _itemService.GetAllCategoriesAsync();
            ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name");
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _itemService.GetByIdAsync(id.Value);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _itemService.GetByIdAsync(id.Value);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("id,name,description,discount,price,imageUrl,category,stock_quantity")] Item item)
        {
            if (id != item.id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _itemService.UpdateItemAsync(item);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_itemService.ItemExists(item.id))
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
            if (id == null) return NotFound();

            var item = await _itemService.GetByIdAsync(id.Value);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _itemService.DeleteItemAsync(id);
            return RedirectToAction(nameof(AdminControl));
        }
    }
}