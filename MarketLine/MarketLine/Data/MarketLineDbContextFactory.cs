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
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets(typeof(MarketLineDbContextFactory).Assembly, optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Set ConnectionStrings:DefaultConnection in user secrets or the ConnectionStrings__DefaultConnection environment variable.");
            }

            var options = new DbContextOptionsBuilder<MarketLineDbContext>();
            options.UseSqlServer(connectionString);
            return new MarketLineDbContext(options.Options);
        }
    }
}
