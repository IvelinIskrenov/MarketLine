using System.Threading.RateLimiting;
using MarketLine.Data;
using MarketLine.Filters;
using MarketLine.Models;
using MarketLine.Security;
using MarketLine.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CartCountFilter>();
    options.Filters.Add<WishlistFilter>();
});

var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var isProduction = builder.Environment.IsProduction();

if (!useInMemory && string.IsNullOrWhiteSpace(connectionString))
{
    if (isProduction)
    {
        throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection in the hosting environment.");
    }

    useInMemory = true;
}

if (!useInMemory)
{
    try
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
    }
    catch (Exception)
    {
        if (isProduction)
        {
            throw new InvalidOperationException("The database is not available.");
        }

        useInMemory = true;
    }
}

builder.Services.AddDbContext<MarketLineDbContext>(options =>
{
    if (useInMemory)
    {
        var inMemoryName = builder.Configuration["InMemoryDatabaseName"] ?? "MarketLineDemo";
        options.UseInMemoryDatabase(inMemoryName);
    }
    else
    {
        options.UseSqlServer(connectionString);
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
});

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<MarketLineDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.IterationCount = 100_000;
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = ".MarketLine.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});

var loginPermitLimit = builder.Configuration.GetValue("Security:LoginPermitLimit", 5);
var loginWindowSeconds = builder.Configuration.GetValue("Security:LoginWindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(AuthMessages.LoginRateLimited, token);
    };
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, loginPermitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, loginWindowSeconds)),
                QueueLimit = 0
            }));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("MarketLine", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Accept", "Content-Type")
            .WithMethods("GET", "POST")
            .AllowCredentials();
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = ".MarketLine.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IShopAdminService, ShopAdminService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
    if (useInMemory)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

if (useInMemory)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();

    if (!db.Items.Any())
    {
        db.Items.AddRange(
            new Item
            {
                name = "Хидратиращ крем за лице",
                description = "Лек крем за ежедневна грижа, подходящ за всеки тип кожа. Овлажнява и защитава кожната бариера.",
                discount = 15,
                price = 24.90m,
                imageUrl = "https://images.unsplash.com/photo-1556228578-0d85b1a4d571?auto=format&fit=crop&w=800&q=80",
                CategoryId = 1,
                stock_quantity = 18
            },
            new Item
            {
                name = "Слънцезащитен флуид SPF 50",
                description = "Неомазняващ флуид с висок фактор, подходящ за лице. Лека текстура и бързо абсорбиране.",
                discount = 0,
                price = 32.50m,
                imageUrl = "https://images.unsplash.com/photo-1556228720-195a9692988c?auto=format&fit=crop&w=800&q=80",
                CategoryId = 1,
                stock_quantity = 12
            },
            new Item
            {
                name = "Лосион за тяло с ший",
                description = "Подхранващ лосион за суха кожа. Оставя кожата мека и еластична без лепкав филм.",
                discount = 10,
                price = 18.99m,
                imageUrl = "https://images.unsplash.com/photo-1608248543803-ba4f8c70e0c6?auto=format&fit=crop&w=800&q=80",
                CategoryId = 2,
                stock_quantity = 25
            },
            new Item
            {
                name = "Душ гел с алое",
                description = "Нежен душ гел за ежедневна употреба с натурално алое вера.",
                discount = 0,
                price = 9.49m,
                imageUrl = "https://images.unsplash.com/photo-1556228453-efd6c1ff04f6?auto=format&fit=crop&w=800&q=80",
                CategoryId = 2,
                stock_quantity = 40
            },
            new Item
            {
                name = "Витамин C 1000 mg",
                description = "Хранителна добавка за имунна подкрепа. 30 таблетки в опаковка.",
                discount = 20,
                price = 15.90m,
                imageUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?auto=format&fit=crop&w=800&q=80",
                CategoryId = 3,
                stock_quantity = 50
            },
            new Item
            {
                name = "Омега-3 капсули",
                description = "Рибено масло с EPA и DHA за сърце и стави. 60 капсули.",
                discount = 0,
                price = 21.00m,
                imageUrl = "https://images.unsplash.com/photo-1471864190282-a93a3070b6de?auto=format&fit=crop&w=800&q=80",
                CategoryId = 3,
                stock_quantity = 33
            }
        );
        db.SaveChanges();
    }
}

using (var scope = app.Services.CreateScope())
{
    ShopSeed.EnsureAsync(scope.ServiceProvider).GetAwaiter().GetResult();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

if (app.Environment.IsProduction())
{
    app.UseForwardedHeaders();
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMarketLineSecurityHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("MarketLine");
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "site-page",
    pattern: "{key}",
    defaults: new { controller = "Home", action = "Page" },
    constraints: new { key = "about|contacts|obshti-uslovia|dostavka-i-plashtane|zashtita-na-lichnite-danni" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
