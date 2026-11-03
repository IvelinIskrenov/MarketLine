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

            await EnsureCatalogAsync(db);

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

        private static async Task EnsureCatalogAsync(MarketLineDbContext db)
        {
            var headphonesId = await EnsureCategoryAsync(db, "Слушалки");
            var phonesId = await EnsureCategoryAsync(db, "Смартфони");
            var laptopsId = await EnsureCategoryAsync(db, "Лаптопи");

            await EnsureProductAsync(db, new Item
            {
                name = "Слушалки Razer BlackShark V2",
                description = "Гейминг слушалки Razer с ясен звук, меки наушници и микрофон за комуникация. Подходящи за компютър и конзола.",
                discount = 10,
                price = 249.90m,
                imageUrl = "https://images.unsplash.com/photo-1618366712010-f4ae9c647dcb?auto=format&fit=crop&w=800&q=80",
                CategoryId = headphonesId,
                Brand = "Razer",
                Features = "Гейминг, Микрофон, Жични",
                CreatedAt = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc)
            },
                new ItemVariant { Color = "Черни", Model = "BlackShark V2", StockQuantity = 8 },
                new ItemVariant { Color = "Зелени", Model = "BlackShark V2", StockQuantity = 6 });

            await EnsureProductAsync(db, new Item
            {
                name = "iPhone 18 Pro",
                description = "Смартфон Apple iPhone 18 Pro с OLED дисплей, тройна камера и 5G. Доставя се отключен, готов за български оператори.",
                discount = 0,
                price = 2499.00m,
                imageUrl = "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80",
                CategoryId = phonesId,
                Brand = "Apple",
                Features = "OLED, 5G, 256 GB",
                CreatedAt = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
            },
                new ItemVariant { Color = "Титан", Model = "256 GB", StockQuantity = 4 },
                new ItemVariant { Color = "Черно", Model = "256 GB", StockQuantity = 5 },
                new ItemVariant { Color = "Бяло", Model = "512 GB", StockQuantity = 3 });

            await EnsureProductAsync(db, new Item
            {
                name = "Серум с витамин C",
                description = "Лек серум за лице с витамин C. Изравнява тена, придава сияние и се впива бързо без лепкав филм.",
                discount = 15,
                price = 36.90m,
                imageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=800&q=80",
                CategoryId = 1,
                Brand = "GlowLab",
                Features = "Серум, Витамин C, За лице",
                CreatedAt = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc)
            },
                new ItemVariant { Size = "30 ml", Model = "Сияние", StockQuantity = 14 });

            await EnsureProductAsync(db, new Item
            {
                name = "Лаптоп ASUS VivoBook 15",
                description = "Лаптоп ASUS с 15.6\" дисплей, Intel процесор и SSD диск. Подходящ за учене, офис и ежедневна работа.",
                discount = 5,
                price = 1299.00m,
                imageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&q=80",
                CategoryId = laptopsId,
                Brand = "ASUS",
                Features = "15.6\", Intel, SSD",
                CreatedAt = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc)
            },
                new ItemVariant { Color = "Сив", Model = "16 GB RAM", StockQuantity = 4 },
                new ItemVariant { Color = "Сив", Model = "32 GB RAM", StockQuantity = 2 });

            await EnsureProductAsync(db, new Item
            {
                name = "Слушалки за телефон Sony",
                description = "Компактни безжични слушалки Sony за телефон. Стабилен Bluetooth, удобна посадка и чист звук за разговори и музика.",
                discount = 0,
                price = 89.90m,
                imageUrl = "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?auto=format&fit=crop&w=800&q=80",
                CategoryId = headphonesId,
                Brand = "Sony",
                Features = "Bluetooth, За телефон, Безжични",
                CreatedAt = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc)
            },
                new ItemVariant { Color = "Бели", Model = "WF-C700N", StockQuantity = 12 },
                new ItemVariant { Color = "Черни", Model = "WF-C700N", StockQuantity = 10 });

            await EnsureProductAsync(db, new Item
            {
                name = "Крем за ръце с ший",
                description = "Подхранващ крем за ръце с масло от ший. Омекотява сухата кожа и се впива бързо, без да оставя мазен филм.",
                discount = 0,
                price = 11.90m,
                imageUrl = "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?auto=format&fit=crop&w=800&q=80",
                CategoryId = 2,
                Brand = "HandSoft",
                Features = "За ръце, Подхранване, Суха кожа",
                CreatedAt = new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc)
            },
                new ItemVariant { Size = "50 ml", Model = "Ший", StockQuantity = 20 },
                new ItemVariant { Size = "100 ml", Model = "Ший", StockQuantity = 15 });

            await EnsureProductAsync(db, new Item
            {
                name = "Крем за хидратиране",
                description = "Лек хидратиращ крем за лице и тяло. Възстановява влагата и оставя кожата мека за целия ден.",
                discount = 10,
                price = 16.50m,
                imageUrl = "https://images.unsplash.com/photo-1611930022073-b7a4ba5fcccd?auto=format&fit=crop&w=800&q=80",
                CategoryId = 1,
                Brand = "AquaDerm",
                Features = "Хидратация, За лице, Дневен",
                CreatedAt = new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc)
            },
                new ItemVariant { Size = "50 ml", Model = "Дневен", StockQuantity = 16 },
                new ItemVariant { Size = "100 ml", Model = "Дневен", StockQuantity = 9 });
        }

        private static async Task<int> EnsureCategoryAsync(MarketLineDbContext db, string name)
        {
            var category = await db.Categories.FirstOrDefaultAsync(current => current.Name == name);
            if (category != null)
            {
                return category.Id;
            }

            category = new Category { Name = name };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category.Id;
        }

        private static async Task EnsureProductAsync(MarketLineDbContext db, Item item, params ItemVariant[] variants)
        {
            if (await db.Items.AnyAsync(current => current.name == item.name))
            {
                return;
            }

            foreach (var variant in variants)
            {
                item.Variants.Add(variant);
            }

            ItemInventoryData.SyncStock(item);
            db.Items.Add(item);
            await db.SaveChangesAsync();
        }
    }
}
