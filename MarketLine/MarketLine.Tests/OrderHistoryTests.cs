using System.Net;
using MarketLine.Data;
using MarketLine.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class OrderHistoryTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public OrderHistoryTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Customer_SeesOwnOrderAndCannotReorderAnotherUsersOrder()
        {
            var owned = await SeedOrderAsync(MarketLineFactory.CustomerEmail, "Хидратиращ крем за лице", 1);
            var foreign = await SeedOrderAsync(MarketLineFactory.OtherEmail, "Омега-3 капсули", 1);
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);

            var list = await client.GetAsync("/Profile/Orders");
            list.EnsureSuccessStatusCode();
            var listHtml = WebUtility.HtmlDecode(await list.Content.ReadAsStringAsync());
            Assert.Contains(owned.OrderNumber, listHtml);
            Assert.DoesNotContain(foreign.OrderNumber, listHtml);
            Assert.Contains("Нова", listHtml);
            Assert.Contains("Повтори поръчката", listHtml);

            var details = await client.GetAsync("/Profile/Order?orderNumber=" + owned.OrderNumber);
            details.EnsureSuccessStatusCode();
            var detailsHtml = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
            Assert.Contains("Проследяване", detailsHtml);
            Assert.Contains("Потвърдена", detailsHtml);
            Assert.Contains("Обработва се", detailsHtml);
            Assert.Contains("Изпратена", detailsHtml);
            Assert.Contains("Доставена", detailsHtml);
            Assert.Contains("Хидратиращ крем за лице", detailsHtml);

            var foreignDetails = await client.GetAsync("/Profile/Order?orderNumber=" + foreign.OrderNumber);
            var foreignHtml = WebUtility.HtmlDecode(await FollowAsync(client, foreignDetails));
            Assert.DoesNotContain(foreign.OrderNumber, foreignHtml);
        }

        [Fact]
        public async Task Customer_CanReorderOwnOrderIntoCart()
        {
            var owned = await SeedOrderAsync(MarketLineFactory.CustomerEmail, "Душ гел с алое", 2);
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Profile/Orders");

            var reorder = await client.PostAsync("/Profile/Reorder", FormRequest.Create(
                token,
                ("orderNumber", owned.OrderNumber)));

            Assert.Equal(HttpStatusCode.Redirect, reorder.StatusCode);
            Assert.Contains("/Cart", reorder.Headers.Location?.OriginalString);

            var cart = await client.GetAsync("/Cart");
            cart.EnsureSuccessStatusCode();
            var cartHtml = WebUtility.HtmlDecode(await cart.Content.ReadAsStringAsync());
            Assert.Contains("Душ гел с алое", cartHtml);
            Assert.Contains("от поръчка", cartHtml);
        }

        [Fact]
        public async Task Customer_CannotReorderAnotherUsersOrder()
        {
            var foreign = await SeedOrderAsync(MarketLineFactory.OtherEmail, "Витамин C 1000 mg", 1);
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Profile/Orders");

            await client.PostAsync("/Profile/Reorder", FormRequest.Create(
                token,
                ("orderNumber", foreign.OrderNumber)));

            var cart = await client.GetAsync("/Cart");
            cart.EnsureSuccessStatusCode();
            var cartHtml = WebUtility.HtmlDecode(await cart.Content.ReadAsStringAsync());
            Assert.DoesNotContain("Витамин C 1000 mg", cartHtml);
        }

        [Fact]
        public async Task Admin_CanListOrdersAndChangeStatus()
        {
            var owned = await SeedOrderAsync(MarketLineFactory.CustomerEmail, "Лосион за тяло с ший", 1);
            var client = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);

            var list = await client.GetAsync("/Admin/Orders");
            list.EnsureSuccessStatusCode();
            var listHtml = WebUtility.HtmlDecode(await list.Content.ReadAsStringAsync());
            Assert.Contains(owned.OrderNumber, listHtml);
            Assert.Contains("Нова", listHtml);
            Assert.Contains("Потвърдена", listHtml);
            Assert.Contains("Отказана", listHtml);

            var filtered = await client.GetAsync("/Admin/Orders?status=" + Uri.EscapeDataString("Нова"));
            filtered.EnsureSuccessStatusCode();
            var filteredHtml = WebUtility.HtmlDecode(await filtered.Content.ReadAsStringAsync());
            Assert.Contains(owned.OrderNumber, filteredHtml);

            var details = await client.GetAsync("/Admin/Order/" + owned.Id);
            details.EnsureSuccessStatusCode();
            var detailsHtml = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
            Assert.Contains("Проследяване", detailsHtml);
            Assert.Contains("Лосион за тяло с ший", detailsHtml);

            var token = FormRequest.ReadToken(detailsHtml);
            var setStatus = await client.PostAsync("/Admin/SetOrderStatus", FormRequest.Create(
                token,
                ("id", owned.Id.ToString()),
                ("status", OrderStatuses.Shipped)));
            Assert.Equal(HttpStatusCode.Redirect, setStatus.StatusCode);

            var updated = await client.GetAsync("/Admin/Order/" + owned.Id);
            updated.EnsureSuccessStatusCode();
            var updatedHtml = WebUtility.HtmlDecode(await updated.Content.ReadAsStringAsync());
            Assert.Contains("Изпратена", updatedHtml);

            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var stored = await db.Orders.FirstAsync(o => o.Id == owned.Id);
            Assert.Equal(OrderStatuses.Shipped, stored.Status);
            Assert.True(stored.StatusManagedByAdmin);
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

        private static async Task<string> FollowAsync(HttpClient client, HttpResponseMessage response)
        {
            if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod)
            {
                response = await client.GetAsync(response.Headers.Location);
            }

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<Order> SeedOrderAsync(string email, string itemName, int quantity)
        {
            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var owner = await users.FindByEmailAsync(email);
            Assert.NotNull(owner);

            var item = db.Items.First(i => i.name == itemName);
            var order = new Order
            {
                OrderNumber = "ML-HIST-" + Guid.NewGuid().ToString("N")[..8],
                UserId = owner.Id,
                CreatedAt = DateTime.Now,
                Status = OrderStatuses.New,
                StatusManagedByAdmin = true,
                PaymentStatus = PaymentStatuses.Pending,
                FirstName = owner.FirstName,
                LastName = owner.LastName,
                Email = owner.Email ?? email,
                Phone = "0888123456",
                Address = "ул. История 1",
                City = "София",
                PostalCode = "1000",
                Courier = "Спиди",
                DeliveryMethod = "до адрес",
                PaymentMethod = "наложен платеж",
                SubTotal = item.price * quantity,
                Shipping = 5.99m,
                Total = item.price * quantity + 5.99m,
                Lines =
                {
                    new OrderLine
                    {
                        ItemId = item.id,
                        Name = item.name!,
                        ImageUrl = item.imageUrl,
                        UnitPrice = item.price,
                        Quantity = quantity
                    }
                }
            };

            db.Orders.Add(order);
            db.SaveChanges();
            return order;
        }
    }
}
