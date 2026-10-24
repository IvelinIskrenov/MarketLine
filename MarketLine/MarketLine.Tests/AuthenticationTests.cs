using System.Net;
using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MarketLine.Tests
{
    public class AuthenticationTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public AuthenticationTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Login_WithValidCredentials_SignsIn()
        {
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login");

            var response = await client.PostAsync("/Account/Login", FormRequest.Create(
                token,
                ("Email", MarketLineFactory.CustomerEmail),
                ("Password", MarketLineFactory.CustomerPassword),
                ("RememberMe", "false")));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Item", response.Headers.Location?.OriginalString);
            Assert.True(HasAuthCookie(response));
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShowsGenericError()
        {
            var client = _factory.CreateAppClient();
            var html = await LoginAndReadAsync(client, MarketLineFactory.CustomerEmail, "wrong-password");

            Assert.Contains(AuthMessages.InvalidLogin, html);
            Assert.DoesNotContain("lockout", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ShowsSameGenericError()
        {
            var client = _factory.CreateAppClient();
            var html = await LoginAndReadAsync(client, "missing@test.com", "wrong-password");

            Assert.Contains(AuthMessages.InvalidLogin, html);
            Assert.DoesNotContain("не съществува", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Logout_ClearsAuthentication()
        {
            var client = await CreateLoggedInClientAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var home = await client.GetAsync("/");
            home.EnsureSuccessStatusCode();
            var token = FormRequest.ReadToken(await home.Content.ReadAsStringAsync());

            var logout = await client.PostAsync("/Account/Logout", FormRequest.Create(token));

            Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
            Assert.Equal("/", logout.Headers.Location?.OriginalString);

            var profile = await client.GetAsync("/Profile");
            Assert.Equal(HttpStatusCode.Redirect, profile.StatusCode);
            Assert.Contains("/Account/Login", profile.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Profile_RequiresAuthentication()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/Profile");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Admin_RequiresAuthentication()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/Admin");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Admin_IsForbidden_ForCustomer()
        {
            var client = await CreateLoggedInClientAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var response = await client.GetAsync("/Admin");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/AccessDenied", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Admin_IsAllowed_ForAdministrator()
        {
            var client = await CreateLoggedInClientAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
            var response = await client.GetAsync("/Admin");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Customer_CannotViewAnotherUsersOrder()
        {
            var orderNumber = await SeedForeignOrderAsync();
            var client = await CreateLoggedInClientAsync(MarketLineFactory.OtherEmail, MarketLineFactory.OtherPassword);

            var profileOrder = await client.GetAsync("/Profile/Order?orderNumber=" + orderNumber);
            var profileHtml = await FollowAndReadAsync(client, profileOrder);
            Assert.DoesNotContain("Secret Street 1", profileHtml);

            var checkout = await client.GetAsync("/Checkout/Success?orderNumber=" + orderNumber);
            Assert.Equal(HttpStatusCode.Redirect, checkout.StatusCode);
            Assert.Contains("/Item", checkout.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Customer_CannotDeleteAnotherUsersAddress()
        {
            var addressId = await SeedForeignAddressAsync();
            var client = await CreateLoggedInClientAsync(MarketLineFactory.OtherEmail, MarketLineFactory.OtherPassword);
            var addressesPage = await client.GetAsync("/Profile/Addresses");
            addressesPage.EnsureSuccessStatusCode();
            var token = FormRequest.ReadToken(await addressesPage.Content.ReadAsStringAsync());

            var delete = await client.PostAsync("/Profile/DeleteAddress", FormRequest.Create(
                token,
                ("id", addressId.ToString())));

            Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);

            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            Assert.True(await db.Addresses.AnyAsync(a => a.Id == addressId));
        }

        [Fact]
        public async Task Passwords_AreHashedWithIdentityHasher()
        {
            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var user = await users.FindByEmailAsync(MarketLineFactory.CustomerEmail);

            Assert.NotNull(user);
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
            Assert.NotEqual(MarketLineFactory.CustomerPassword, user.PasswordHash);
            Assert.Equal(
                PasswordVerificationResult.Success,
                hasher.VerifyHashedPassword(user, user.PasswordHash, MarketLineFactory.CustomerPassword));
        }

        [Fact]
        public async Task ForgotPassword_DoesNotRevealWhetherEmailExists()
        {
            var known = _factory.CreateAppClient();
            var unknown = _factory.CreateAppClient();

            var knownResponse = await PostForgotPasswordAsync(known, MarketLineFactory.CustomerEmail);
            var unknownResponse = await PostForgotPasswordAsync(unknown, "missing@test.com");

            Assert.Equal(knownResponse.StatusCode, unknownResponse.StatusCode);
            Assert.Equal(knownResponse.Headers.Location?.OriginalString, unknownResponse.Headers.Location?.OriginalString);
            Assert.Contains("/Account/ResetPassword", knownResponse.Headers.Location?.OriginalString);

            var knownHtml = await FollowAndReadAsync(known, knownResponse);
            var unknownHtml = await FollowAndReadAsync(unknown, unknownResponse);
            Assert.Contains("Нова парола", knownHtml);
            Assert.Contains("Нова парола", unknownHtml);
            Assert.Contains("name=\"Token\"", knownHtml);
            Assert.DoesNotContain("name=\"Token\" type=\"hidden\" value=\"CfDJ", knownHtml);
        }

        [Fact]
        public async Task ResetPassword_WorksForRegisteredEmail()
        {
            var client = _factory.CreateAppClient();
            try
            {
                var forgot = await PostForgotPasswordAsync(client, MarketLineFactory.OtherEmail);
                var resetPage = await client.GetAsync(forgot.Headers.Location);
                resetPage.EnsureSuccessStatusCode();
                var token = FormRequest.ReadToken(await resetPage.Content.ReadAsStringAsync());

                var reset = await client.PostAsync("/Account/ResetPassword", FormRequest.Create(
                    token,
                    ("Email", MarketLineFactory.OtherEmail),
                    ("Password", "novaParola1"),
                    ("ConfirmPassword", "novaParola1")));

                Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);
                Assert.Contains("/Account/Login", reset.Headers.Location?.OriginalString);

                var loginHtml = await LoginAndReadAsync(client, MarketLineFactory.OtherEmail, MarketLineFactory.OtherPassword);
                Assert.Contains(AuthMessages.InvalidLogin, loginHtml);

                var newLogin = await client.PostAsync("/Account/Login", FormRequest.Create(
                    await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login"),
                    ("Email", MarketLineFactory.OtherEmail),
                    ("Password", "novaParola1"),
                    ("RememberMe", "false")));

                Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
            }
            finally
            {
                RestoreOtherPassword();
            }
        }

        [Fact]
        public void AuthCookie_IsHttpOnly_WithExpiration()
        {
            _factory.CreateAppClient();
            var options = _factory.Services
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(IdentityConstants.ApplicationScheme);

            Assert.True(options.Cookie.HttpOnly);
            Assert.Equal(".MarketLine.Auth", options.Cookie.Name);
            Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
            Assert.True(options.SlidingExpiration);
            Assert.Equal("/Account/Login", options.LoginPath.Value);
            Assert.Equal("/Account/AccessDenied", options.AccessDeniedPath.Value);
        }

        private async Task<HttpClient> CreateLoggedInClientAsync(string email, string password)
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

        private static async Task<string> LoginAndReadAsync(HttpClient client, string email, string password)
        {
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login");
            var response = await client.PostAsync("/Account/Login", FormRequest.Create(
                token,
                ("Email", email),
                ("Password", password),
                ("RememberMe", "false")));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        }

        private static async Task<HttpResponseMessage> PostForgotPasswordAsync(HttpClient client, string email)
        {
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/ForgotPassword");
            return await client.PostAsync("/Account/ForgotPassword", FormRequest.Create(
                token,
                ("Email", email)));
        }

        private static async Task<string> FollowAndReadAsync(HttpClient client, HttpResponseMessage response)
        {
            if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod)
            {
                response = await client.GetAsync(response.Headers.Location);
            }

            return await response.Content.ReadAsStringAsync();
        }

        private static bool HasAuthCookie(HttpResponseMessage response)
        {
            return response.Headers.TryGetValues("Set-Cookie", out var cookies)
                && cookies.Any(cookie => cookie.Contains(".MarketLine.Auth") && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<string> SeedForeignOrderAsync()
        {
            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var owner = await users.FindByEmailAsync(MarketLineFactory.CustomerEmail);
            Assert.NotNull(owner);

            var orderNumber = "ML-TEST-" + Guid.NewGuid().ToString("N")[..8];
            db.Orders.Add(new Order
            {
                OrderNumber = orderNumber,
                UserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatuses.Accepted,
                PaymentStatus = PaymentStatuses.Pending,
                FirstName = "Owner",
                LastName = "Customer",
                Email = owner.Email ?? MarketLineFactory.CustomerEmail,
                Phone = "0888000000",
                Address = "Secret Street 1",
                City = "Sofia",
                PostalCode = "1000",
                Courier = "Спиди",
                DeliveryMethod = "до адрес",
                PaymentMethod = "наложен платеж",
                Total = 20m
            });
            await db.SaveChangesAsync();
            return orderNumber;
        }

        private async Task<int> SeedForeignAddressAsync()
        {
            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var owner = await users.FindByEmailAsync(MarketLineFactory.CustomerEmail);
            Assert.NotNull(owner);

            var address = new Address
            {
                UserId = owner.Id,
                AddressLine = "Secret Street 1",
                City = "Sofia",
                PostalCode = "1000",
                IsDefault = true
            };
            db.Addresses.Add(address);
            await db.SaveChangesAsync();
            return address.Id;
        }

        private void RestoreOtherPassword()
        {
            using var scope = _factory.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = users.FindByEmailAsync(MarketLineFactory.OtherEmail).GetAwaiter().GetResult();
            Assert.NotNull(user);
            var reset = users.GeneratePasswordResetTokenAsync(user).GetAwaiter().GetResult();
            var result = users.ResetPasswordAsync(user, reset, MarketLineFactory.OtherPassword).GetAwaiter().GetResult();
            Assert.True(result.Succeeded);
        }
    }
}
