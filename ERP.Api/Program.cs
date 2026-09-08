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
using ERP.Services; // SeedService para seeding inicial

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE BASE DE DATOS ---
// Pasillo universal: si ya existe GestionX.db activo (tenant.json), usarlo como DB por empresa (aislamiento físico por PC/empresa)
// Si no, usar DefaultConnection (programa vacío con bootstrap admin@erp.local)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("Database:UseSqlite");
try
{
    var tenantMarker = Path.Combine(AppContext.BaseDirectory, "tenant.json");
    // Buscar también junto al .db actual
    if (!File.Exists(tenantMarker))
    {
        var altDir = Path.GetDirectoryName(connectionString?.Contains("Data Source=") == true ? connectionString.Split("Data Source=")[1].Split(';')[0].Trim() : "");
        if (!string.IsNullOrEmpty(altDir) && !Path.IsPathRooted(altDir)) altDir = Path.Combine(AppContext.BaseDirectory, altDir);
        var altMarker = !string.IsNullOrEmpty(altDir) ? Path.Combine(Path.GetDirectoryName(altDir) ?? AppContext.BaseDirectory, "tenant.json") : null;
        if (altMarker != null && File.Exists(altMarker)) tenantMarker = altMarker;
    }
    if (File.Exists(tenantMarker))
    {
        var json = File.ReadAllText(tenantMarker);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("ActiveDatabase", out var active) && active.GetString() is string activeFile && !string.IsNullOrWhiteSpace(activeFile))
        {
            var tenantPath = Path.Combine(Path.GetDirectoryName(tenantMarker)!, activeFile);
            if (File.Exists(tenantPath))
            {
                connectionString = $"Data Source={tenantPath}";
                useSqlite = true;
                Console.WriteLine($"[Tenant] Usando BBDD por empresa: {tenantPath}");
            }
        }
    }
}
catch { /* si falla, seguir con DefaultConnection vacía (pasillo) */ }
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
// Módulos legales 2026 - Dossier §3-9
builder.Services.AddScoped<ERP.Services.Bancario.BancarioService>();
builder.Services.AddScoped<ERP.Services.Bancario.SepaXmlGeneratorService>();
builder.Services.AddScoped<ERP.Services.Contabilidad.ContabilidadService>();
builder.Services.AddScoped<ERP.Services.Trazabilidad.TrazabilidadService>();
builder.Services.AddScoped<ERP.Services.Fiscal.MotorIVAService>();
// Onboarding multi-tenant: GestionX.db por empresa (miles de PCs/empresas)
builder.Services.AddScoped<ERP.Services.Tenant.TenantDatabaseService>();

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
        await SeedService.SeedAsync(context);

        // --- 8b. SEED BOOTSTRAP: credenciales iniciales para BBDD vacía ---
        // No crea empresa demo. El primer usuario entra con credenciales iniciales
        // y desde la UI crea la empresa, familias, artículos, etc.
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var bootstrapEmail = "admin@erp.local";
        var bootstrap = await userManager.FindByEmailAsync(bootstrapEmail);
        if (bootstrap == null)
        {
            bootstrap = new ApplicationUser
            {
                UserName = bootstrapEmail,
                Email = bootstrapEmail,
                FullName = "Administrador Inicial",
                EmpresaId = null, // bootstrap sin empresa; la creará tras el primer login
                IsActivo = true,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(bootstrap, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(bootstrap, "Admin");
                foreach (var perm in AppPermissions.All)
                    await userManager.AddClaimAsync(bootstrap, new System.Security.Claims.Claim("Permission", perm));
            }
            else
            {
                var loggerSeed = services.GetRequiredService<ILogger<Program>>();
                loggerSeed.LogError("No se pudo crear usuario bootstrap: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

        // --- 8c. DETECCIÓN DE ONBOARDING NECESARIO ---
        // Si existe usuario bootstrap y no hay empresas, el próximo login forzará onboarding
        if (bootstrap != null && !await context.Empresas.AnyAsync())
        {
            // Marcamos que el onboarding está pendiente usando una key en la configuración
            // o simplemente dejamos el EmpresaId=null en el usuario bootstrap
            // y el cliente Blazor detectará esta condición al cargar
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Onboarding necesario: usuario bootstrap existe, pero no hay empresas registradas.");
            logger.LogInformation("El próximo login redirigirá al asistente de configuración de empresa.");
        }
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
