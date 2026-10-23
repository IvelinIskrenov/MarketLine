using MarketLine.Data;
using MarketLine.Filters;
using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CartCountFilter>();
    options.Filters.Add<WishlistFilter>();
});

var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!useInMemory)
{
    try
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
    }
    catch (Exception)
    {
        useInMemory = true;
    }
}

builder.Services.AddDbContext<MarketLineDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("MarketLineDemo");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<MarketLineDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
