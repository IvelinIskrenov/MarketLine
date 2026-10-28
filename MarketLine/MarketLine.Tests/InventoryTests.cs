using System.Net;
using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class InventoryTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public InventoryTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Catalog_ShowsSoldOutAndVariantChoice()
        {
            var client = _factory.CreateAppClient();
            var catalog = await client.GetAsync("/Item");
            catalog.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await catalog.Content.ReadAsStringAsync());
            Assert.Contains("Избери вариант", html);
            Assert.Contains("В наличност", html);

            await SetItemStockAsync("Омега-3 капсули", 0);
            try
            {
                var soldOut = await client.GetAsync("/Item");
                soldOut.EnsureSuccessStatusCode();
                var soldOutHtml = WebUtility.HtmlDecode(await soldOut.Content.ReadAsStringAsync());
                Assert.Contains("Изчерпан", soldOutHtml);
            }
            finally
            {
                await SetItemStockAsync("Омега-3 капсули", 33);
            }
        }

        [Fact]
        public async Task Cart_RejectsSoldOutProduct()
        {
            await SetItemStockAsync("Омега-3 капсули", 0);
            try
            {
                var client = _factory.CreateAppClient();
                var item = await FindItemAsync("Омега-3 капсули");
                var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Item/Details/" + item.id);
                var add = await client.PostAsync("/Cart/Add", FormRequest.Create(
                    token,
                    ("id", item.id.ToString()),
                    ("quantity", "1")));
                Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);

                var cart = await client.GetAsync("/Cart");
                cart.EnsureSuccessStatusCode();
                var html = WebUtility.HtmlDecode(await cart.Content.ReadAsStringAsync());
                Assert.Contains("изчерпан", html, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Омега-3 капсули", html);
            }
            finally
            {
                await SetItemStockAsync("Омега-3 капсули", 33);
            }
        }

        [Fact]
        public async Task Details_ShowsVariantsAndLowStockWarning()
        {
            var item = await FindItemAsync("Хидратиращ крем за лице");
            var small = item.Variants.First(current => current.Size == "50 ml");
            var large = item.Variants.First(current => current.Size == "100 ml");
            await SetVariantStockAsync(small.Id, 2);
            await SetVariantStockAsync(large.Id, 2);
            try
            {
                var client = _factory.CreateAppClient();
                var details = await client.GetAsync("/Item/Details/" + item.id);
                details.EnsureSuccessStatusCode();
                var html = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
                Assert.Contains("50 ml", html);
                Assert.Contains("100 ml", html);
                Assert.Contains("остават", html);
                Assert.Contains("Ниска наличност", html);
            }
            finally
            {
                await SetVariantStockAsync(small.Id, 10);
                await SetVariantStockAsync(large.Id, 8);
            }
        }

        [Fact]
        public async Task Order_DecrementsStockAndCancelRestoresIt()
        {
            var item = await FindItemAsync("Омега-3 капсули");
            var variant = item.Variants.Single();
            var before = variant.StockQuantity;

            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var owner = await users.FindByEmailAsync(MarketLineFactory.CustomerEmail);
            Assert.NotNull(owner);

            var form = new CheckoutForm
            {
                FirstName = "Иван",
                LastName = "Петров",
                Email = MarketLineFactory.CustomerEmail,
                Phone = "0888123456",
                Address = "ул. Склад 1",
                City = "София",
                PostalCode = "1000",
                Courier = "Спиди",
                DeliveryMethod = "до адрес",
                PaymentMethod = "наложен платеж"
            };
            var cart = new CartViewModel
            {
                Lines =
                {
                    new CartLine
                    {
                        ItemId = item.id,
                        VariantId = variant.Id,
                        VariantLabel = variant.Label,
                        Name = item.name,
                        UnitPrice = item.price,
                        Quantity = 2,
                        StockQuantity = before
                    }
                },
                SubTotal = item.price * 2,
                Shipping = 5.99m,
                Total = item.price * 2 + 5.99m
            };

            var (order, error) = await orders.PlaceAsync(form, cart, owner.Id);
            Assert.Null(error);
            Assert.NotNull(order);

            using (var checkScope = _factory.CreateScope())
            {
                var db = checkScope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
                var stored = await db.ItemVariants.FirstAsync(current => current.Id == variant.Id);
                Assert.Equal(before - 2, stored.StockQuantity);
                var parent = await db.Items.FirstAsync(current => current.id == item.id);
                Assert.Equal(before - 2, parent.stock_quantity);
            }

            var oversellCart = new CartViewModel
            {
                Lines =
                {
                    new CartLine
                    {
                        ItemId = item.id,
                        VariantId = variant.Id,
                        Name = item.name,
                        Quantity = before,
                        UnitPrice = item.price
                    }
                }
            };
            var failed = await orders.PlaceAsync(form, oversellCart, owner.Id);
            Assert.Null(failed.Order);
            Assert.Contains("изчерпан", failed.Error, StringComparison.OrdinalIgnoreCase);

            await orders.CancelAsync(order.Id);
            using var restoreScope = _factory.CreateScope();
            var restoreDb = restoreScope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var restored = await restoreDb.ItemVariants.FirstAsync(current => current.Id == variant.Id);
            Assert.Equal(before, restored.StockQuantity);
        }

        [Fact]
        public async Task Admin_SeesInventoryVariantsAndLowStockAlert()
        {
            var item = await FindItemAsync("Лосион за тяло с ший");
            var variant = item.Variants.First();
            await SetVariantStockAsync(variant.Id, 3);
            try
            {
                var client = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
                var inventory = await client.GetAsync("/Admin/Inventory");
                inventory.EnsureSuccessStatusCode();
                var inventoryHtml = WebUtility.HtmlDecode(await inventory.Content.ReadAsStringAsync());
                Assert.Contains("200 ml", inventoryHtml);
                Assert.Contains("Ниска наличност", inventoryHtml);
                Assert.Contains("Добави вариант", inventoryHtml);

                var dashboard = await client.GetAsync("/Admin");
                dashboard.EnsureSuccessStatusCode();
                var dashboardHtml = WebUtility.HtmlDecode(await dashboard.Content.ReadAsStringAsync());
                Assert.Contains("Предупреждения за наличност", dashboardHtml);
                Assert.Contains("Лосион за тяло с ший", dashboardHtml);
            }
            finally
            {
                await SetVariantStockAsync(variant.Id, 15);
            }
        }

        private async Task<HttpClient> LoginAsync(string email, string password)
        {
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login");
            var response = await client.PostAsync("/Account/Login", FormRequest.Create(
                token,
                ("Email", email),
                ("Password", password),
                ("RememberMe", "false")));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            return client;
        }

        private async Task<Item> FindItemAsync(string name)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var item = await db.Items.Include(current => current.Variants).FirstAsync(current => current.name == name);
            return item;
        }

        private async Task SetItemStockAsync(string name, int quantity)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var item = await db.Items.Include(current => current.Variants).FirstAsync(current => current.name == name);
            foreach (var variant in item.Variants)
            {
                variant.StockQuantity = quantity;
            }

            ItemInventoryData.SyncStock(item);
            await db.SaveChangesAsync();
        }

        private async Task SetVariantStockAsync(int variantId, int quantity)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var variant = await db.ItemVariants
                .Include(current => current.Item)
                .ThenInclude(item => item!.Variants)
                .FirstAsync(current => current.Id == variantId);
            variant.StockQuantity = quantity;
            if (variant.Item != null)
            {
                ItemInventoryData.SyncStock(variant.Item);
            }

            await db.SaveChangesAsync();
        }
    }
}
