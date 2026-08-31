using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Configuration;
using ERP.Data;

namespace ERP.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(basePath, "..", "ERP.Api"))
                .AddJsonFile("appsettings.Development.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Resolve relative path to absolute path based on ERP.Api directory
            if (connectionString.StartsWith("Data Source=..\\"))
            {
                var apiDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ERP.Api"));
                var dbFile = connectionString.Substring("Data Source=..\\".Length);
                var fullPath = Path.GetFullPath(Path.Combine(apiDir, dbFile));
                connectionString = $"Data Source={fullPath}";
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}