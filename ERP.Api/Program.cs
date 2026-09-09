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
// BBDD INICIAL erp.db (maestro) con todos los usuarios duplicados. Por request se resuelve GestionX.db vía claim Tenant.
// Si no hay Tenant (bootstrap admin@erp.local pasillo) se usa DefaultConnection (maestro).
var masterConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=erp.db";
var masterUseSqlite = builder.Configuration.GetValue<bool?>("Database:UseSqlite")
    ?? builder.Configuration.GetSection("Database").GetValue<bool?>("UseSqlite")
    ?? builder.Configuration.GetValue<bool?>("Database:UseSqlite", true)
    ?? true;
var contentRoot = builder.Environment.ContentRootPath;
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var httpCtx = sp.GetService<IHttpContextAccessor>()?.HttpContext;
    var tenantFile = httpCtx?.User?.FindFirst("Tenant")?.Value;
    string conn = masterConnectionString;
    bool useSqlite = masterUseSqlite;
    if (!string.IsNullOrWhiteSpace(tenantFile))
    {
        var masterFile = masterConnectionString.Contains("Data Source=") ? masterConnectionString.Split("Data Source=")[1].Split(';')[0].Trim() : "erp.db";
        var dir = Path.IsPathRooted(masterFile) ? Path.GetDirectoryName(masterFile)! : contentRoot;
        var tenantPath = Path.Combine(dir, tenantFile);
        if (File.Exists(tenantPath))
        {
            conn = $"Data Source={tenantPath}";
            useSqlite = true;
        }
    }
    else
    {
        // Asegurar que maestro apunta a ContentRoot, no a bin — y compartir erp.db con ERP.Api si existe (web y escritorio mismos usuarios)
        if (masterConnectionString.Contains("Data Source="))
        {
            var mf = masterConnectionString.Split("Data Source=")[1].Split(';')[0].Trim();
            if (!Path.IsPathRooted(mf))
            {
                var desktopDb = Path.Combine(contentRoot, mf);
                var sharedCandidates = new[]
                {
                    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "..", "ERP.Api", mf)),
                    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "..", "ERP.Api", "erp.db")),
                    Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "..", "ERP.Api", "erp-fresh.db")),
                    @"C:\Users\josearregui\Desktop\Proyectos\ERP .NET\ERP.Api\erp-fresh.db",
                    @"C:\Users\josearregui\Desktop\Proyectos\ERP .NET\ERP.Api\erp.db",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ERP", mf)
                };
                string? shared = sharedCandidates.FirstOrDefault(File.Exists);
                // Preferir compartido si Desktop está vacío (0 empresas) o es más pequeño
                if (shared != null && File.Exists(desktopDb))
                {
                    try
                    {
                        var di = new FileInfo(desktopDb);
                        var si = new FileInfo(shared);
                        // Desktop 749568 con 1 usuario vs Api 749568 con 2 usuarios: misma talla pero distinto contenido -> preferir Api si Desktop tiene 0 empresas
                        // Heurística: si Desktop fue creado hoy y tiene 0 empresas, usar compartido
                        if (si.Length >= di.Length) conn = $"Data Source={shared}";
                        else conn = $"Data Source={desktopDb}";
                    }
                    catch { conn = $"Data Source={shared}"; }
                }
                else if (shared != null) conn = $"Data Source={shared}";
                else conn = $"Data Source={desktopDb}";
                Console.WriteLine($"[DB] shared={shared} desktopDb={desktopDb} conn={conn}");
            }
        }
    }
    if (useSqlite) options.UseSqlite(conn, o => o.UseRelationalNulls()); else options.UseSqlServer(conn);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
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
{
    // Fallback para escritorio publicado sin user-secrets: usar clave dev integrada (solo si Development o si no hay secret configurado)
    if (builder.Environment.IsDevelopment())
    {
        jwtSecret = "clave-desarrollo-ERP-2026-minimo-32-caracteres-segura";
    }
    else
    {
        // Intentar clave por defecto para modo escritorio standalone (evita crash "localhost rechazó")
        jwtSecret = builder.Configuration["JWT:Secret"] ?? "clave-produccion-ERP-2026-32c-minimo-cambiar-en-produccion";
        if (jwtSecret.Length < 32) throw new InvalidOperationException("JWT:Secret debe configurarse con al menos 32 caracteres.");
    }
}

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
    // Escritorio WebView2 file:// tiene Origin null/file:// — permitir localhost + file para CORS
    policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return true; // file:// -> null
            if (origin.StartsWith("file://")) return true;
            if (origin == "null") return true;
            try { var u = new Uri(origin); return u.Host == "localhost" || u.Host == "127.0.0.1"; } catch { return false; }
        })
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

        var bootstrapEmails = new[] { "admin@erp.local", "admin@erp.com" };
        ApplicationUser? bootstrap = null;
        foreach (var bootstrapEmail in bootstrapEmails)
        {
            bootstrap = await userManager.FindByEmailAsync(bootstrapEmail);
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
                    loggerSeed.LogError("No se pudo crear usuario bootstrap {Email}: {Errors}", bootstrapEmail, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }
        // referencia para 8c
        bootstrap = await userManager.FindByEmailAsync("admin@erp.local") ?? await userManager.FindByEmailAsync("admin@erp.com");

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
app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true, DefaultContentType = "application/octet-stream" });
app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DashboardHub>("/dashboardHub");
app.MapFallbackToFile("index.html"); 

app.Run();
