using System.Net;
using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class PublishingTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public PublishingTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ScheduledProduct_IsHiddenFromCatalogUntilPublishTime()
        {
            var item = await SeedScheduledAsync("Планиран нощен крем");
            try
            {
                var client = _factory.CreateAppClient();
                var catalog = await client.GetAsync("/Item");
                catalog.EnsureSuccessStatusCode();
                var catalogHtml = WebUtility.HtmlDecode(await catalog.Content.ReadAsStringAsync());
                Assert.DoesNotContain(item.name!, catalogHtml);

                var details = await client.GetAsync("/Item/Details/" + item.id);
                Assert.Equal(HttpStatusCode.NotFound, details.StatusCode);

                var suggest = await client.GetAsync("/Item/Suggest?q=" + Uri.EscapeDataString("Планиран"));
                suggest.EnsureSuccessStatusCode();
                var suggestHtml = await suggest.Content.ReadAsStringAsync();
                Assert.DoesNotContain("Планиран нощен крем", suggestHtml);

                await SetPublishedAtAsync(item.id, DateTime.UtcNow.AddMinutes(-5));

                var liveCatalog = await client.GetAsync("/Item");
                liveCatalog.EnsureSuccessStatusCode();
                var liveHtml = WebUtility.HtmlDecode(await liveCatalog.Content.ReadAsStringAsync());
                Assert.Contains("Планиран нощен крем", liveHtml);

                var liveDetails = await client.GetAsync("/Item/Details/" + item.id);
                liveDetails.EnsureSuccessStatusCode();
            }
            finally
            {
                await DeleteItemAsync(item.id);
            }
        }

        [Fact]
        public async Task Admin_CanScheduleProductAndSeeItInAdminPanel()
        {
            var client = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Item/Create");
            var publishAt = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd'T'HH:mm");
            var name = "Планиран админ крем";

            var create = await client.PostAsync("/Item/Create", FormRequest.Create(
                token,
                ("name", name),
                ("description", "Ще се появи по-късно."),
                ("Brand", "MarketLine"),
                ("Features", "Тест"),
                ("price", "12.50"),
                ("discount", "0"),
                ("CategoryId", "1"),
                ("stock_quantity", "4"),
                ("publishAt", publishAt)));
            Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

            var admin = await client.GetAsync("/Item/AdminControl");
            admin.EnsureSuccessStatusCode();
            var adminHtml = WebUtility.HtmlDecode(await admin.Content.ReadAsStringAsync());
            Assert.Contains(name, adminHtml);
            Assert.Contains("Планиран", adminHtml);

            var dashboard = await client.GetAsync("/Admin");
            dashboard.EnsureSuccessStatusCode();
            var dashboardHtml = WebUtility.HtmlDecode(await dashboard.Content.ReadAsStringAsync());
            Assert.Contains("Планирани продукти", dashboardHtml);

            var guest = _factory.CreateAppClient();
            var catalog = await guest.GetAsync("/Item");
            catalog.EnsureSuccessStatusCode();
            var catalogHtml = WebUtility.HtmlDecode(await catalog.Content.ReadAsStringAsync());
            Assert.DoesNotContain(name, catalogHtml);

            var created = await FindItemAsync(name);
            var details = await client.GetAsync("/Item/Details/" + created.id);
            details.EnsureSuccessStatusCode();
            var detailsHtml = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
            Assert.Contains("още не се вижда в каталога", detailsHtml);

            await DeleteItemAsync(created.id);
        }

        [Fact]
        public async Task Create_RejectsInvalidPublishDate()
        {
            var client = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Item/Create");
            var create = await client.PostAsync("/Item/Create", FormRequest.Create(
                token,
                ("name", "Невалидна дата крем"),
                ("description", "Тест"),
                ("price", "10"),
                ("discount", "0"),
                ("CategoryId", "1"),
                ("stock_quantity", "1"),
                ("publishAt", "not-a-date")));

            Assert.Equal(HttpStatusCode.OK, create.StatusCode);
            var html = WebUtility.HtmlDecode(await create.Content.ReadAsStringAsync());
            Assert.Contains("Невалидна дата за публикуване", html);

            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            Assert.False(await db.Items.AnyAsync(item => item.name == "Невалидна дата крем"));
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

        private async Task<Item> SeedScheduledAsync(string name)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var item = new Item
            {
                name = name,
                description = "Скрит до датата на публикуване.",
                price = 19.90m,
                CategoryId = 1,
                stock_quantity = 5,
                Brand = "MarketLine",
                CreatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow.AddDays(3)
            };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            ItemInventoryData.EnsureVariants(item);
            await db.SaveChangesAsync();
            return item;
        }

        private async Task<Item> FindItemAsync(string name)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            return await db.Items.FirstAsync(item => item.name == name);
        }

        private async Task SetPublishedAtAsync(int id, DateTime? publishedAt)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var item = await db.Items.FirstAsync(current => current.id == id);
            item.PublishedAt = publishedAt;
            await db.SaveChangesAsync();
        }

        private async Task DeleteItemAsync(int id)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var item = await db.Items.Include(current => current.Variants).FirstOrDefaultAsync(current => current.id == id);
            if (item != null)
            {
                db.Items.Remove(item);
                await db.SaveChangesAsync();
            }
        }
    }
}
