using MarketLine.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketLine.Filters
{
    public class CartCountFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller)
            {
                var cartService = context.HttpContext.RequestServices.GetRequiredService<ICartService>();
                controller.ViewBag.CartCount = cartService.GetCount();

                try
                {
                    var admin = context.HttpContext.RequestServices.GetRequiredService<IShopAdminService>();
                    var shipping = await admin.GetShippingAsync();
                    controller.ViewBag.FreeShippingThreshold = shipping.FreeOver;
                }
                catch
                {
                    controller.ViewBag.FreeShippingThreshold = 100m;
                }
            }

            await next();
        }
    }
}
