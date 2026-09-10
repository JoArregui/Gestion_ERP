using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ERP.Data;
using ERP.Services;
using Microsoft.AspNetCore.Identity;

var builder = Host.CreateApplicationBuilder(args);

// Use absolute path for the database
var dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "erp-fresh.db"));
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<SeedService>();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    // Apply migrations
    await context.Database.MigrateAsync();
    Console.WriteLine("Migrations applied successfully.");

    // Seed data
    var seedService = services.GetRequiredService<SeedService>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    await SeedService.SeedAsync(context, userManager, roleManager);
    Console.WriteLine("Seeding completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("Done.");
Console.ReadLine();