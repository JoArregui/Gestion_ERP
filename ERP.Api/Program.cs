using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Services;
using ERP.Api.Hubs;
using ERP.Api.Services; // IEmailService / EmailService siguen aquí (Api-specific infra)
using ERP.Domain.Constants;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE BASE DE DATOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("Database:UseSqlite");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});

// --- 2. CONFIGURACIÓN DE IDENTITY ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- 3. CONFIGURACIÓN DE SEGURIDAD JWT ---
var jwtSecret = builder.Configuration["JWT:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT:Secret debe configurarse con al menos 32 caracteres.");

var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "ERP.Api";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "ERP.Web";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero 
    };
});

// --- 4. POLÍTICAS DE AUTORIZACIÓN DINÁMICAS (BASADAS EN CLAIMS) ---
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy => 
            policy.RequireClaim("Permission", permission));
    }
});

// --- 5. POLÍTICA DE CORS ---
builder.Services.AddCors(options =>
{
options.AddPolicy("AllowBlazorClient", policy =>
{
    policy.WithOrigins("http://localhost:5053", "https://localhost:5053", "http://localhost:5109", "https://localhost:5109")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});
});

// --- 6. REGISTRO DE SERVICIOS DE NEGOCIO ---
builder.Services.AddSignalR();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<NominaService>();
builder.Services.AddScoped<RRHHService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ComprasService>();
builder.Services.AddScoped<CicloFacturacionService>();
builder.Services.AddScoped<FacturacionService>();
builder.Services.AddScoped<VerifactuService>();

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();

// --- 7. SWAGGER ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ERP Profesional API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- 8. SEEDING AUTOMÁTICO ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // FIX 26/08: EnsureCreated no aplica migraciones -> SQLite quedaba sin columna Estado (AddEstadoDocumento)
        // y provocaba SQLite Error 1: 'no such column: d.Estado' en api/facturacion/listado y api/CicloFacturacion
        // Se usa MigrateAsync para ambos proveedores; EnsureCreated solo como fallback si no hay historial.
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception migrateEx)
        {
            var loggerMigrate = services.GetRequiredService<ILogger<Program>>();
            loggerMigrate.LogWarning(migrateEx, "MigrateAsync falló, intentando EnsureCreated como fallback.");
            await context.Database.EnsureCreatedAsync();
        }
        await SeedService.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error crítico en la fase de migración o seeding.");
    }
}

// --- 9. MIDDLEWARE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP API v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DashboardHub>("/dashboardHub");
app.MapFallbackToFile("index.html"); 

app.Run();
