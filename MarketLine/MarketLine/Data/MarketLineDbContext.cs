using Microsoft.EntityFrameworkCore;
using MarketLine.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MarketLine.Data
{
    public class MarketLineDbContext : IdentityDbContext<User>
    {
        public MarketLineDbContext(DbContextOptions<MarketLineDbContext> options) : base(options) { }

        public DbSet<Item> Items { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Address> Addresses { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderLine> OrderLines { get; set; }

        public DbSet<WishlistItem> WishlistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Лице" },
                new Category { Id = 2, Name = "Тяло" },
                new Category { Id = 3, Name = "Хранително добавки" }
                );

            base.OnModelCreating(builder);

            builder.Entity<Address>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.ItemId })
                .IsUnique();

            builder.Entity<WishlistItem>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasOne(w => w.Item)
                .WithMany()
                .HasForeignKey(w => w.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            builder.Entity<Order>()
                .Property(o => o.SubTotal)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .Property(o => o.PromoDiscount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .Property(o => o.Shipping)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Order>()
                .HasMany(o => o.Lines)
                .WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderLine>()
                .Property(l => l.UnitPrice)
                .HasColumnType("decimal(18,2)");
        }
    }
}
