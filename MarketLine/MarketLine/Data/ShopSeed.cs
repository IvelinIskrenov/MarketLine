using MarketLine.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Data
{
    public static class ShopSeed
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<MarketLineDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<User>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count == 0)
            {
                var firstUser = await userManager.Users.OrderBy(u => u.Email).FirstOrDefaultAsync();
                if (firstUser != null)
                {
                    await userManager.AddToRoleAsync(firstUser, "Admin");
                }
            }

            if (!await db.PromoCodes.AnyAsync())
            {
                db.PromoCodes.AddRange(
                    new PromoCode { Code = "WELCOME10", IsPercent = true, Value = 10, IsActive = true },
                    new PromoCode { Code = "SAVE5", IsPercent = false, Value = 5, MinSubTotal = 5, IsActive = true }
                );
            }

            if (!await db.Couriers.AnyAsync())
            {
                db.Couriers.AddRange(
                    new Courier { Name = "Спиди", Price = 5.99m, IsActive = true },
                    new Courier { Name = "Еконт", Price = 6.49m, IsActive = true }
                );
            }

            if (!await db.ShippingSettings.AnyAsync())
            {
                db.ShippingSettings.Add(new ShippingSettings { DefaultPrice = 5.99m, FreeOver = 100m });
            }

            if (!await db.SitePages.AnyAsync())
            {
                db.SitePages.AddRange(
                    new SitePage
                    {
                        Key = "home",
                        Title = "Добре дошли в MarketLine",
                        Body = "Козметика, грижа за кожата и хранителни добавки с безплатна доставка над 100 лв.",
                        ImageUrl = "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?auto=format&fit=crop&w=1400&q=80"
                    },
                    new SitePage
                    {
                        Key = "about",
                        Title = "За нас",
                        Body = "MarketLine е онлайн магазин за козметика и хранителни добавки. Предлагаме проверени марки, бърза доставка и лесно пазаруване."
                    },
                    new SitePage
                    {
                        Key = "contacts",
                        Title = "Контакти",
                        Body = "Имейл: support@marketline.bg\nТелефон: 0888 123 456\nРаботно време: всеки ден от 9:00 до 18:00."
                    },
                    new SitePage
                    {
                        Key = "obshti-uslovia",
                        Title = "Общи условия",
                        Body = "Поръчките се обработват след потвърждение. Клиентът може да се откаже от поръчка преди изпращане. Връщане на стока е възможно в законовия срок, ако продуктът е в оригинално състояние."
                    },
                    new SitePage
                    {
                        Key = "dostavka-i-plashtane",
                        Title = "Доставка и плащане",
                        Body = "Доставяме със Спиди и Еконт. Стандартната цена е 5.99 лв. При поръчка над 100 лв. доставката е безплатна. Плащане: наложен платеж или банков превод."
                    },
                    new SitePage
                    {
                        Key = "zashtita-na-lichnite-danni",
                        Title = "Защита на личните данни",
                        Body = "Личните данни се използват само за обработка на поръчки и управление на профила. Не ги предоставяме на трети страни извън куриерските и платежните услуги."
                    }
                );
            }

            foreach (var item in await db.Items.Include(i => i.Variants).ToListAsync())
            {
                if (string.IsNullOrWhiteSpace(item.Brand))
                {
                    item.CreatedAt = default;
                }

                ItemCatalogData.ApplyDefaults(item);
                ItemInventoryData.EnsureVariants(item);
            }

            await db.SaveChangesAsync();
        }
    }
}
