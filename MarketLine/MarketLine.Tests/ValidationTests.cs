using System.Net;
using MarketLine.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class ValidationTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public ValidationTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Register_RejectsInvalidEmailAndLongName()
        {
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Register");

            var invalidEmail = await client.PostAsync("/Account/Register", FormRequest.Create(
                token,
                ("FirstName", "Иван"),
                ("LastName", "Петров"),
                ("Email", "not-an-email"),
                ("Password", "parola1"),
                ("ConfirmPassword", "parola1")));

            var emailHtml = WebUtility.HtmlDecode(await invalidEmail.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, invalidEmail.StatusCode);
            Assert.Contains("валиден имейл", emailHtml, StringComparison.OrdinalIgnoreCase);

            var longName = await client.PostAsync("/Account/Register", FormRequest.Create(
                await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Register"),
                ("FirstName", new string('A', 21)),
                ("LastName", "Петров"),
                ("Email", "longname@test.com"),
                ("Password", "parola1"),
                ("ConfirmPassword", "parola1")));

            Assert.Equal(HttpStatusCode.OK, longName.StatusCode);
            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            Assert.Null(await users.FindByEmailAsync("longname@test.com"));
        }

        [Fact]
        public async Task Register_IgnoresIsAdminAndRoleFields()
        {
            var client = _factory.CreateAppClient();
            var email = $"safe{Guid.NewGuid():N}@test.com";
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Register");

            var response = await client.PostAsync("/Account/Register", FormRequest.Create(
                token,
                ("FirstName", "Иван"),
                ("LastName", "Петров"),
                ("Email", email),
                ("Password", "parola1"),
                ("ConfirmPassword", "parola1"),
                ("IsAdmin", "true"),
                ("Roles", "Admin"),
                ("UserRoles", "Admin")));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.False(user.IsAdmin);
            Assert.False(await users.IsInRoleAsync(user, "Admin"));
        }

        [Fact]
        public async Task Profile_IgnoresIsAdminAndDoesNotExposePasswordHash()
        {
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var page = await client.GetAsync("/Profile");
            page.EnsureSuccessStatusCode();
            var html = await page.Content.ReadAsStringAsync();
            Assert.DoesNotContain("PasswordHash", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AQAAAA", html);

            var token = FormRequest.ReadToken(html);
            await client.PostAsync("/Profile", FormRequest.Create(
                token,
                ("FirstName", "Клиент"),
                ("LastName", "Един"),
                ("Email", MarketLineFactory.CustomerEmail),
                ("Phone", ""),
                ("IsAdmin", "true")));

            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await users.FindByEmailAsync(MarketLineFactory.CustomerEmail);
            Assert.NotNull(user);
            Assert.False(user.IsAdmin);
            Assert.False(await users.IsInRoleAsync(user, "Admin"));
        }

        [Fact]
        public async Task Customer_CannotChangeRolesThroughUserEdit()
        {
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Profile");
            var response = await client.PostAsync("/User/Edit/abc", FormRequest.Create(
                token,
                ("Id", "abc"),
                ("FirstName", "Hacker"),
                ("LastName", "User"),
                ("UserRoles", "Admin")));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Cart_RejectsInvalidProductIdAndQuantity()
        {
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Item/Details/1");

            var badId = await client.PostAsync("/Cart/Add", FormRequest.Create(
                token,
                ("id", "0"),
                ("quantity", "1")));
            Assert.Equal(HttpStatusCode.Redirect, badId.StatusCode);

            var badQuantity = await client.PostAsync("/Cart/Add", FormRequest.Create(
                token,
                ("id", "1"),
                ("quantity", "1000")));
            Assert.Equal(HttpStatusCode.Redirect, badQuantity.StatusCode);

            var cart = await client.GetAsync("/Cart");
            cart.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await cart.Content.ReadAsStringAsync());
            Assert.Contains("Невалидн", html);
        }

        [Fact]
        public async Task Checkout_RejectsMissingCustomerDetails()
        {
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Item/Details/1");
            await client.PostAsync("/Cart/Add", FormRequest.Create(
                token,
                ("id", "1"),
                ("quantity", "1")));

            var checkout = await client.GetAsync("/Checkout");
            checkout.EnsureSuccessStatusCode();
            var checkoutToken = FormRequest.ReadToken(await checkout.Content.ReadAsStringAsync());

            var response = await client.PostAsync("/Checkout", FormRequest.Create(
                checkoutToken,
                ("FirstName", ""),
                ("LastName", ""),
                ("Email", "bad"),
                ("Phone", "x"),
                ("Address", ""),
                ("City", ""),
                ("PostalCode", "!!"),
                ("Courier", ""),
                ("DeliveryMethod", ""),
                ("PaymentMethod", "")));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Contains("задължител", html);
        }

        [Fact]
        public async Task Address_RejectsEmptyAndOversizedValues()
        {
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var page = await client.GetAsync("/Profile/Addresses");
            page.EnsureSuccessStatusCode();
            var token = FormRequest.ReadToken(await page.Content.ReadAsStringAsync());

            var response = await client.PostAsync("/Profile/AddAddress", FormRequest.Create(
                token,
                ("NewAddress.Label", "Дом"),
                ("NewAddress.AddressLine", ""),
                ("NewAddress.City", new string('C', 90)),
                ("NewAddress.PostalCode", "??")));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Contains("задължителен", html);
        }

        [Fact]
        public async Task DisplayedUserInput_IsHtmlEncoded()
        {
            var client = _factory.CreateAppClient();
            var email = $"xss{Guid.NewGuid():N}@test.com";
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Register");
            var register = await client.PostAsync("/Account/Register", FormRequest.Create(
                token,
                ("FirstName", "<script>x"),
                ("LastName", "Петров"),
                ("Email", email),
                ("Password", "parola1"),
                ("ConfirmPassword", "parola1")));
            Assert.Equal(HttpStatusCode.Redirect, register.StatusCode);

            var profile = await client.GetAsync("/Profile");
            profile.EnsureSuccessStatusCode();
            var html = await profile.Content.ReadAsStringAsync();
            Assert.DoesNotContain("<script>x", html);
            Assert.Contains("&lt;script&gt;x", html);
        }

        [Fact]
        public async Task Cors_DoesNotAllowUnknownOrigins()
        {
            var client = _factory.CreateAppClient();
            client.DefaultRequestHeaders.Add("Origin", "https://evil.example");
            var response = await client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var allowed = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
                && values.Contains("https://evil.example");
            Assert.False(allowed);
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
    }
}
