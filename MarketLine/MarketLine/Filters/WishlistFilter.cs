using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketLine.Filters
{
    public class WishlistFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller)
            {
                var ids = new HashSet<int>();
                var user = context.HttpContext.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                    var userId = userManager.GetUserId(user);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var wishlist = context.HttpContext.RequestServices.GetRequiredService<IWishlistService>();
                        ids = await wishlist.GetItemIdsAsync(userId);
                    }
                }

                controller.ViewBag.WishlistItemIds = ids;
            }

            await next();
        }
    }
}
