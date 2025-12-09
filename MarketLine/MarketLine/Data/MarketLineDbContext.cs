using Microsoft.EntityFrameworkCore; //9.0.4 version
using MarketLine.Models;
namespace MarketLine.Data
{
    public class MarketLineDbContext : DbContext
    {
        public MarketLineDbContext(DbContextOptions<MarketLineDbContext> options) : base(options) { }

        public DbSet<Item> Items { get; set; }
    }
}
