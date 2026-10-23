using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MarketLine.Data
{
    public class MarketLineDbContextFactory : IDesignTimeDbContextFactory<MarketLineDbContext>
    {
        public MarketLineDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var options = new DbContextOptionsBuilder<MarketLineDbContext>();
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            return new MarketLineDbContext(options.Options);
        }
    }
}
