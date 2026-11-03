using System.Net;
using MarketLine.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class CatalogSeedTests : IClassFixture<MarketLineFactory>
    {
        private static readonly string[] NewProducts =
        {
            "Слушалки Razer BlackShark V2",
            "iPhone 18 Pro",
            "Серум с витамин C",
            "Лаптоп ASUS VivoBook 15",
            "Слушалки за телефон Sony",
            "Крем за ръце с ший",
            "Крем за хидратиране"
        };

        private readonly MarketLineFactory _factory;

        public CatalogSeedTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Catalog_ShowsNewProductsWithPriceAndImage()
        {
            using (var scope = _factory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
                foreach (var name in NewProducts)
                {
                    var item = await db.Items.Include(i => i.Variants).FirstOrDefaultAsync(i => i.name == name);
                    Assert.NotNull(item);
                    Assert.False(string.IsNullOrWhiteSpace(item.description));
                    Assert.False(string.IsNullOrWhiteSpace(item.imageUrl));
                    Assert.True(item.price > 0);
                    Assert.True(item.AvailableStock > 0);
                    Assert.NotNull(item.Brand);
                }
            }

            var client = _factory.CreateAppClient();
            var catalog = await client.GetAsync("/Item");
            catalog.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await catalog.Content.ReadAsStringAsync());
            foreach (var name in NewProducts)
            {
                Assert.Contains(name, html);
            }
        }
    }
}
