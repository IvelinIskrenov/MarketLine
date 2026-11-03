using System.Net;
using MarketLine.Localization;

namespace MarketLine.Tests
{
    public class LocalizationTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public LocalizationTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Translator_UsesOnePhraseTableForUiAndContent()
        {
            Assert.Equal("Продукти", ShopTranslator.Ui("nav.products", false));
            Assert.Equal("Products", ShopTranslator.Ui("nav.products", true));
            Assert.Equal("Хидратиращ крем за лице", ShopTranslator.Content("Хидратиращ крем за лице", false));
            Assert.Equal("Hydrating face cream", ShopTranslator.Content("Хидратиращ крем за лице", true));
            Assert.Equal("€12.50", 12.5m.Euro());
        }

        [Fact]
        public async Task Catalog_DefaultsToBulgarianAndEuroPrices()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/Item");
            response.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

            Assert.Contains("Продукти", html);
            Assert.Contains("Хидратиращ крем за лице", html);
            Assert.Contains("€", html);
            Assert.DoesNotContain("$", html);
            Assert.Contains(">BG</a>", html);
            Assert.Contains(">EN</a>", html);
        }

        [Fact]
        public async Task LanguageSwitch_ShowsEnglishNamesWithoutChangingStoredValues()
        {
            var client = _factory.CreateAppClient();
            var switchResponse = await client.GetAsync("/Language/Set?culture=en&returnUrl=/Item");
            Assert.Equal(HttpStatusCode.Redirect, switchResponse.StatusCode);

            var response = await client.GetAsync("/Item");
            response.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

            Assert.Contains("Products", html);
            Assert.Contains("Hydrating face cream", html);
            Assert.Contains("Filters", html);
            Assert.Contains("€", html);
            Assert.DoesNotContain("Хидратиращ крем за лице", html);
        }

        [Fact]
        public async Task EnglishSearch_FindsBulgarianCatalogNames()
        {
            var client = _factory.CreateAppClient();
            await client.GetAsync("/Language/Set?culture=en&returnUrl=/Item");

            var response = await client.GetAsync("/Item?searchTerm=" + Uri.EscapeDataString("headphones"));
            response.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

            Assert.Contains("Razer BlackShark V2 headphones", html);
        }
    }
}
