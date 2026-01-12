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
            var categories = await _context.Items
                .Select(x => x.category)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return View(categories);
        }
    }
}
