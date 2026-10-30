using System.Net;
using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class StockAlertTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public StockAlertTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Guest_CanSubscribeToSoldOutProduct()
        {
            await SetItemStockAsync("Омега-3 капсули", 0);
            try
            {
                var item = await FindItemAsync("Омега-3 капсули");
                var client = _factory.CreateAppClient();
                var details = await client.GetAsync("/Item/Details/" + item.id);
                details.EnsureSuccessStatusCode();
                var detailsHtml = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
                Assert.Contains("Извести ме", detailsHtml);
                Assert.Contains("Извести ме при наличност", detailsHtml);

                var catalog = await client.GetAsync("/Item");
                catalog.EnsureSuccessStatusCode();
                var catalogHtml = WebUtility.HtmlDecode(await catalog.Content.ReadAsStringAsync());
                Assert.Contains("Извести ме", catalogHtml);

                var token = FormRequest.ReadToken(detailsHtml);
                var subscribe = await client.PostAsync("/StockAlert/Subscribe", FormRequest.Create(
                    token,
                    ("itemId", item.id.ToString()),
                    ("variantId", item.Variants[0].Id.ToString()),
                    ("email", "wait@test.com"),
                    ("returnUrl", "/Item/Details/" + item.id)));
                Assert.Equal(HttpStatusCode.Redirect, subscribe.StatusCode);

                using (var scope = _factory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
                    var alert = await db.StockAlerts.FirstOrDefaultAsync(current => current.Email == "wait@test.com");
                    Assert.NotNull(alert);
                    Assert.Equal(item.id, alert.ItemId);
                    Assert.Null(alert.NotifiedAt);
                }

                var again = await client.PostAsync("/StockAlert/Subscribe", FormRequest.Create(
                    token,
                    ("itemId", item.id.ToString()),
                    ("variantId", item.Variants[0].Id.ToString()),
                    ("email", "wait@test.com")));
                Assert.Equal(HttpStatusCode.Redirect, again.StatusCode);

                using (var scope = _factory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
                    Assert.Equal(1, db.StockAlerts.Count(current => current.Email == "wait@test.com"));
                }
            }
            finally
            {
                await SetItemStockAsync("Омега-3 капсули", 33);
            }
        }

        [Fact]
        public async Task Subscribe_RejectsInvalidEmailAndInStockProduct()
        {
            var item = await FindItemAsync("Омега-3 капсули");
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Item/Details/" + item.id);

            var invalid = await client.PostAsync("/StockAlert/Subscribe", FormRequest.Create(
                token,
                ("itemId", item.id.ToString()),
                ("email", "not-an-email")));
            Assert.Equal(HttpStatusCode.Redirect, invalid.StatusCode);
            var invalidPage = await client.GetAsync("/Item/Details/" + item.id);
            var invalidHtml = WebUtility.HtmlDecode(await invalidPage.Content.ReadAsStringAsync());
            Assert.Contains("валиден имейл", invalidHtml, StringComparison.OrdinalIgnoreCase);

            var inStock = await client.PostAsync("/StockAlert/Subscribe", FormRequest.Create(
                token,
                ("itemId", item.id.ToString()),
                ("email", "ok@test.com")));
            var inStockPage = await client.GetAsync("/Item/Details/" + item.id);
            var inStockHtml = WebUtility.HtmlDecode(await inStockPage.Content.ReadAsStringAsync());
            Assert.Contains("вече е в наличност", inStockHtml);
        }

        [Fact]
        public async Task Restock_SendsEmailAndMarksAlertNotified()
        {
            await SetItemStockAsync("Витамин C 1000 mg", 0);
            var inbox = _factory.Services.GetRequiredService<IEmailInbox>();
            inbox.Clear();
            try
            {
                var item = await FindItemAsync("Витамин C 1000 mg");
                using (var scope = _factory.CreateScope())
                {
                    var alerts = scope.ServiceProvider.GetRequiredService<IStockAlertService>();
                    var error = await alerts.SubscribeAsync(item.id, item.Variants[0].Id, "restock@test.com", null);
                    Assert.Null(error);
                }

                using (var restockScope = _factory.CreateScope())
                {
                    var admin = restockScope.ServiceProvider.GetRequiredService<IShopAdminService>();
                    await admin.UpdateVariantStockAsync(item.Variants[0].Id, 12);
                }

                var emails = inbox.Messages.Where(message => message.To == "restock@test.com").ToList();
                Assert.Single(emails);
                Assert.Contains("отново е в наличност", emails[0].Subject);
                Assert.Contains("Витамин C 1000 mg", emails[0].Body);
                Assert.Contains("/Item/Details/" + item.id, emails[0].Body);

                using var checkScope = _factory.CreateScope();
                var db = checkScope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
                var stored = await db.StockAlerts.FirstAsync(alert => alert.Email == "restock@test.com");
                Assert.NotNull(stored.NotifiedAt);
            }
            finally
            {
                await SetItemStockAsync("Витамин C 1000 mg", 50);
            }
        }

        private async Task<Item> FindItemAsync(string name)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            return await db.Items.Include(current => current.Variants).FirstAsync(current => current.name == name);
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
    }
}
