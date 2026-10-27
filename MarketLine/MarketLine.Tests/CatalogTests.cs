using System.Net;
using System.Net.Http.Json;
using MarketLine.Data;
using MarketLine.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class CatalogTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public CatalogTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Search_FindsProductByNameAndDescription()
        {
            var client = _factory.CreateAppClient();

            var byName = await client.GetAsync("/Item?searchTerm=" + Uri.EscapeDataString("Хидратиращ"));
            byName.EnsureSuccessStatusCode();
            var nameHtml = WebUtility.HtmlDecode(await byName.Content.ReadAsStringAsync());
            Assert.Contains("Хидратиращ крем за лице", nameHtml);
            Assert.DoesNotContain("Омега-3 капсули", nameHtml);

            var byDescription = await client.GetAsync("/Item?searchTerm=" + Uri.EscapeDataString("овлажнява"));
            byDescription.EnsureSuccessStatusCode();
            var descriptionHtml = WebUtility.HtmlDecode(await byDescription.Content.ReadAsStringAsync());
            Assert.Contains("Хидратиращ крем за лице", descriptionHtml);
        }

        [Fact]
        public async Task Filters_ApplyPriceCategoryBrandAndFeature()
        {
            var client = _factory.CreateAppClient();

            var byBrand = await client.GetAsync("/Item?brand=VitaPlus");
            byBrand.EnsureSuccessStatusCode();
            var brandHtml = WebUtility.HtmlDecode(await byBrand.Content.ReadAsStringAsync());
            Assert.Contains("Витамин C 1000 mg", brandHtml);
            Assert.Contains("Омега-3 капсули", brandHtml);
            Assert.DoesNotContain("Хидратиращ крем за лице", brandHtml);

            var byCategory = await client.GetAsync("/Item?category=" + Uri.EscapeDataString("Лице"));
            byCategory.EnsureSuccessStatusCode();
            var categoryHtml = WebUtility.HtmlDecode(await byCategory.Content.ReadAsStringAsync());
            Assert.Contains("Хидратиращ крем за лице", categoryHtml);
            Assert.Contains("Слънцезащитен флуид SPF 50", categoryHtml);
            Assert.DoesNotContain("Душ гел с алое", categoryHtml);

            var byFeature = await client.GetAsync("/Item?feature=" + Uri.EscapeDataString("SPF 50"));
            byFeature.EnsureSuccessStatusCode();
            var featureHtml = WebUtility.HtmlDecode(await byFeature.Content.ReadAsStringAsync());
            Assert.Contains("Слънцезащитен флуид SPF 50", featureHtml);
            Assert.DoesNotContain("Омега-3 капсули", featureHtml);

            var byPrice = await client.GetAsync("/Item?minPrice=20&maxPrice=25");
            byPrice.EnsureSuccessStatusCode();
            var priceHtml = WebUtility.HtmlDecode(await byPrice.Content.ReadAsStringAsync());
            Assert.Contains("Хидратиращ крем за лице", priceHtml);
            Assert.Contains("Омега-3 капсули", priceHtml);
            Assert.DoesNotContain("Слънцезащитен флуид SPF 50", priceHtml);
        }

        [Fact]
        public async Task Sort_OrdersByPriceNewestAndPopularity()
        {
            var client = _factory.CreateAppClient();

            var priceAsc = await client.GetAsync("/Item?sort=price-asc");
            priceAsc.EnsureSuccessStatusCode();
            var priceHtml = WebUtility.HtmlDecode(await priceAsc.Content.ReadAsStringAsync());
            Assert.True(IndexOf(priceHtml, "Душ гел с алое") < IndexOf(priceHtml, "Слънцезащитен флуид SPF 50"));

            var newest = await client.GetAsync("/Item?sort=newest");
            newest.EnsureSuccessStatusCode();
            var newestHtml = WebUtility.HtmlDecode(await newest.Content.ReadAsStringAsync());
            Assert.True(IndexOf(newestHtml, "Хидратиращ крем за лице") < IndexOf(newestHtml, "Омега-3 капсули"));

            using (var scope = _factory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
                var omega = db.Items.First(i => i.name == "Омега-3 капсули");
                db.Orders.Add(new Order
                {
                    OrderNumber = "POP-" + Guid.NewGuid().ToString("N")[..8],
                    CreatedAt = DateTime.UtcNow,
                    Status = OrderStatuses.Accepted,
                    PaymentStatus = PaymentStatuses.Pending,
                    FirstName = "Тест",
                    LastName = "Клиент",
                    Email = "popular@test.com",
                    Phone = "0888123456",
                    Address = "ул. Тест 1",
                    City = "София",
                    PostalCode = "1000",
                    Courier = "Спиди",
                    DeliveryMethod = "до адрес",
                    PaymentMethod = "наложен платеж",
                    SubTotal = omega.price,
                    Shipping = 5.99m,
                    Total = omega.price + 5.99m,
                    Lines =
                    {
                        new OrderLine
                        {
                            ItemId = omega.id,
                            Name = omega.name!,
                            UnitPrice = omega.price,
                            Quantity = 12
                        }
                    }
                });
                db.SaveChanges();
            }

            var popular = await client.GetAsync("/Item?sort=popular");
            popular.EnsureSuccessStatusCode();
            var popularHtml = WebUtility.HtmlDecode(await popular.Content.ReadAsStringAsync());
            Assert.True(IndexOf(popularHtml, "Омега-3 капсули") < IndexOf(popularHtml, "Хидратиращ крем за лице"));
        }

        [Fact]
        public async Task Suggest_ReturnsMatchingProducts()
        {
            var client = _factory.CreateAppClient();

            var empty = await client.GetAsync("/Item/Suggest?q=х");
            empty.EnsureSuccessStatusCode();
            var emptyItems = await empty.Content.ReadFromJsonAsync<List<ItemSuggestion>>();
            Assert.NotNull(emptyItems);
            Assert.Empty(emptyItems);

            var response = await client.GetAsync("/Item/Suggest?q=" + Uri.EscapeDataString("хидр"));
            response.EnsureSuccessStatusCode();
            var items = await response.Content.ReadFromJsonAsync<List<ItemSuggestion>>();
            Assert.NotNull(items);
            Assert.Contains(items, item => item.Name.Contains("Хидратиращ", StringComparison.OrdinalIgnoreCase));
        }

        private static int IndexOf(string html, string value)
        {
            var index = html.IndexOf(value, StringComparison.Ordinal);
            Assert.True(index >= 0, $"Expected to find '{value}' in the catalog page.");
            return index;
        }
    }
}
