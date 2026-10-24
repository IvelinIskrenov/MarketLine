using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLine.Data;

namespace MarketLine.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly MarketLineDbContext _context;

        public CategoryMenuViewComponent(MarketLineDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }
    }
}
