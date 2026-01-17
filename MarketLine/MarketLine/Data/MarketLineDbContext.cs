using Microsoft.EntityFrameworkCore; //9.0.4 version
using MarketLine.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MarketLine.Data
{
    public class MarketLineDbContext : IdentityDbContext<User>
    {
        public MarketLineDbContext(DbContextOptions<MarketLineDbContext> options) : base(options) { }

        public DbSet<Item> Items { get; set; }

        public DbSet<Category> Categories { get; set; }

        // The DbSets for Users, Roles, UserRoles, and other Identity tables are inherited automatically and do not need to be added manually!

        protected override void OnModelCreating(ModelBuilder builder)
        {
            ///summery
            ///Creating Categories which we will use in our app when we create the model
            ///summery
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Лице" },
                new Category { Id = 2, Name = "Тяло" },
                new Category { Id = 3, Name = "Хранително добавки" }
                );
            // MANDATORY: The base method is called to configure the Identity tables
            base.OnModelCreating(builder);

            // Optional additional configurations, fields:
            // builder.Entity<Item>()
            //     .Property(i => i.price)
            //     .HasColumnType("decimal(18, 2)");
        }
    }
}
