using System.Net;
using System.Text;
using MarketLine.Data;
using MarketLine.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class SalesReportTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public SalesReportTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Guest_IsRedirectedFromReports()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/Admin/Reports");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Customer_CannotOpenReports()
        {
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var response = await client.GetAsync("/Admin/Reports");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/AccessDenied", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Admin_SeesTurnoverOrdersAndProductsForPeriod()
        {
            var today = await SeedOrderAsync(
                MarketLineFactory.CustomerEmail,
                "Хидратиращ крем за лице",
                2,
                DateTime.Now,
                OrderStatuses.Confirmed,
                PaymentStatuses.Paid);
            await SeedOrderAsync(
                MarketLineFactory.CustomerEmail,
                "Душ гел с алое",
                1,
                DateTime.Now,
                OrderStatuses.Cancelled,
                PaymentStatuses.Pending);
            var old = await SeedOrderAsync(
                MarketLineFactory.OtherEmail,
                "Омега-3 капсули",
                3,
                DateTime.Now.AddDays(-40),
                OrderStatuses.Delivered,
                PaymentStatuses.Paid);

            var client = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
            var todayReport = await client.GetAsync("/Admin/Reports?period=today");
            todayReport.EnsureSuccessStatusCode();
            var todayHtml = WebUtility.HtmlDecode(await todayReport.Content.ReadAsStringAsync());
            Assert.Contains("Отчети за продажби", todayHtml);
            Assert.Contains(today.OrderNumber, todayHtml);
            Assert.Contains("Хидратиращ крем за лице", todayHtml);
            Assert.DoesNotContain(old.OrderNumber, todayHtml);
            Assert.Contains("Оборот", todayHtml);
            Assert.Contains("По периоди", todayHtml);
            Assert.Contains("Продукти", todayHtml);
            Assert.Contains("Изтегли CSV", todayHtml);

            var all = await client.GetAsync("/Admin/Reports?period=all");
            all.EnsureSuccessStatusCode();
            var allHtml = WebUtility.HtmlDecode(await all.Content.ReadAsStringAsync());
            Assert.Contains(today.OrderNumber, allHtml);
            Assert.Contains(old.OrderNumber, allHtml);
            Assert.Contains("Омега-3 капсули", allHtml);

            var from = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd");
            var to = DateTime.Now.ToString("yyyy-MM-dd");
            var custom = await client.GetAsync($"/Admin/Reports?period=custom&from={from}&to={to}");
            custom.EnsureSuccessStatusCode();
            var customHtml = WebUtility.HtmlDecode(await custom.Content.ReadAsStringAsync());
            Assert.Contains(today.OrderNumber, customHtml);
            Assert.DoesNotContain(old.OrderNumber, customHtml);
        }

        [Fact]
        public async Task Admin_CanExportCsvAndCancelledOrdersAreExcludedFromTurnover()
        {
            var paid = await SeedOrderAsync(
                MarketLineFactory.CustomerEmail,
                "Витамин C 1000 mg",
                1,
                DateTime.Now,
                OrderStatuses.Delivered,
                PaymentStatuses.Paid);
            await SeedOrderAsync(
                MarketLineFactory.CustomerEmail,
                "Лосион за тяло с ший",
                1,
                DateTime.Now,
                OrderStatuses.Cancelled,
                PaymentStatuses.Refunded);

            var client = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
            var export = await client.GetAsync("/Admin/ExportReports?period=today");
            export.EnsureSuccessStatusCode();
            Assert.Equal("text/csv", export.Content.Headers.ContentType?.MediaType);
            var csv = WebUtility.HtmlDecode(Encoding.UTF8.GetString(await export.Content.ReadAsByteArrayAsync()));
            Assert.Contains(paid.OrderNumber, csv);
            Assert.Contains("Витамин C 1000 mg", csv);
            Assert.Contains("Оборот", csv);
            Assert.Contains("Отказани", csv);
            Assert.DoesNotContain("Лосион за тяло с ший", csv);
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

        private async Task<Order> SeedOrderAsync(
            string email,
            string itemName,
            int quantity,
            DateTime createdAt,
            string status,
            string paymentStatus)
        {
            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var owner = await users.FindByEmailAsync(email);
            Assert.NotNull(owner);

            var item = db.Items.First(i => i.name == itemName);
            var order = new Order
            {
                OrderNumber = "ML-REP-" + Guid.NewGuid().ToString("N")[..8],
                UserId = owner.Id,
                CreatedAt = createdAt,
                Status = status,
                StatusManagedByAdmin = true,
                PaymentStatus = paymentStatus,
                FirstName = owner.FirstName,
                LastName = owner.LastName,
                Email = owner.Email ?? email,
                Phone = "0888123456",
                Address = "ул. Отчет 1",
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
            await db.SaveChangesAsync();
            return order;
        }
    }
}
