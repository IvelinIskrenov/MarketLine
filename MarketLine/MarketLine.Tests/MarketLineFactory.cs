using MarketLine.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class MarketLineFactory : WebApplicationFactory<Program>
    {
        public const string CustomerEmail = "customer@test.com";
        public const string CustomerPassword = "parola1";
        public const string OtherEmail = "other@test.com";
        public const string OtherPassword = "parola1";
        public const string AdminEmail = "admin@test.com";
        public const string AdminPassword = "parola1";

        private readonly string _databaseName = "MarketLineTests-" + Guid.NewGuid();
        private bool _seeded;

        public int LoginPermitLimit { get; init; } = 20;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(FindContentRoot());
            builder.UseSetting("UseInMemoryDatabase", "true");
            builder.UseSetting("InMemoryDatabaseName", _databaseName);
            builder.UseSetting("Security:LoginPermitLimit", LoginPermitLimit.ToString());
            builder.UseSetting("Security:LoginWindowSeconds", "60");
        }

        public HttpClient CreateAppClient()
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            EnsureUsers();
            return client;
        }

        public void EnsureUsers()
        {
            if (_seeded)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!roles.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                roles.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
            }

            CreateUser(users, CustomerEmail, CustomerPassword, "Клиент", "Един");
            CreateUser(users, OtherEmail, OtherPassword, "Клиент", "Два");
            var admin = CreateUser(users, AdminEmail, AdminPassword, "Админ", "Панел");
            if (!users.IsInRoleAsync(admin, "Admin").GetAwaiter().GetResult())
            {
                users.AddToRoleAsync(admin, "Admin").GetAwaiter().GetResult();
            }

            _seeded = true;
        }

        public IServiceScope CreateScope()
        {
            EnsureUsers();
            return Services.CreateScope();
        }

        private static User CreateUser(
            UserManager<User> users,
            string email,
            string password,
            string firstName,
            string lastName)
        {
            var existing = users.FindByEmailAsync(email).GetAwaiter().GetResult();
            if (existing != null)
            {
                return existing;
            }

            var user = new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow
            };

            var result = users.CreateAsync(user, password).GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            return user;
        }

        private static string FindContentRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "MarketLine", "MarketLine.csproj");
                if (File.Exists(candidate))
                {
                    return Path.GetDirectoryName(candidate)!;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not find the MarketLine web project.");
        }
    }
}
