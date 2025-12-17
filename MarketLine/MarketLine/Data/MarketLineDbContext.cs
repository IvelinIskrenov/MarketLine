using Microsoft.EntityFrameworkCore; //9.0.4 version
using MarketLine.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MarketLine.Data
{
    public class MarketLineDbContext : IdentityDbContext<User>
    {
        public MarketLineDbContext(DbContextOptions<MarketLineDbContext> options) : base(options) { }

        public DbSet<Item> Items { get; set; }

        // The DbSets for Users, Roles, UserRoles, and other Identity tables are inherited automatically and do not need to be added manually!

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // MANDATORY: The base method is called to configure the Identity tables
            base.OnModelCreating(builder);

            // Optional additional configurations, fields:
            // builder.Entity<Item>()
            //     .Property(i => i.price)
            //     .HasColumnType("decimal(18, 2)");
        }
    }
}
