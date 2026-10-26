using System.Net;

namespace MarketLine.Tests
{
    public class HostingSecurityTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public HostingSecurityTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Responses_IncludeContentTypeAndReferrerHeaders()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            Assert.Equal("nosniff", GetHeader(response, "X-Content-Type-Options"));
            Assert.Equal("strict-origin-when-cross-origin", GetHeader(response, "Referrer-Policy"));
        }

        [Fact]
        public async Task TestingEnvironment_DoesNotRedirectHttpToHttps()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False((int)response.StatusCode is 301 or 302 or 307 or 308
                && response.Headers.Location?.Scheme == "https");
        }

        [Fact]
        public async Task ErrorPage_DoesNotExposeSecretsOrStackTraces()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/Home/Error");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            Assert.DoesNotContain("SQLEXPRESS", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ConnectionString", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stack trace", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("at MarketLine.", html);
            Assert.DoesNotContain("Development Mode", html);
        }

        [Fact]
        public async Task Login_StillWorks_AfterHostingHardening()
        {
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login");
            var response = await client.PostAsync("/Account/Login", FormRequest.Create(
                token,
                ("Email", MarketLineFactory.CustomerEmail),
                ("Password", MarketLineFactory.CustomerPassword),
                ("RememberMe", "false")));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Item", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Profile_StillRequiresAuthentication()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/Profile");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        private static string? GetHeader(HttpResponseMessage response, string name)
        {
            return response.Headers.TryGetValues(name, out var values)
                ? values.FirstOrDefault()
                : (response.Content.Headers.TryGetValues(name, out var contentValues)
                    ? contentValues.FirstOrDefault()
                    : null);
        }
    }
}
