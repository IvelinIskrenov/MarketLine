using MarketLine.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketLine.Filters
{
    public class CartCountFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is Controller controller)
            {
                var cartService = context.HttpContext.RequestServices.GetRequiredService<ICartService>();
                controller.ViewBag.CartCount = cartService.GetCount();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
