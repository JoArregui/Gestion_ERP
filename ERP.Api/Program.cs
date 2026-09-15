using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Services;
using ERP.Api.Hubs;
using ERP.Api.Infrastructure;
using ERP.Api.Services; // IEmailService / EmailService siguen aquí (Api-specific infra)
using ERP.Domain.Constants;

var builder = WebApplication.CreateBuilder(args);

// Necesario para que ApplicationDbContext derive el tenant de la identidad
// autenticada y aplique sus filtros globales de EmpresaId.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddSingleton<ITenantDatabasePathResolver, TenantDatabasePathResolver>();
builder.Services.AddSingleton<TenantDatabaseProvisioner>();

// --- 1. CONFIGURACIÓN DE BASE DE DATOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("Database:UseSqlite");
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var tenantId = sp.GetRequiredService<ITenantContext>().EmpresaId;
    var tenantPathResolver = sp.GetRequiredService<ITenantDatabasePathResolver>();
    var effectiveConnection = connectionString;
    if (tenantId is > 0)
    {
        var tenantPath = tenantPathResolver.GetPath(tenantId.Value);
        if (!File.Exists(tenantPath))
            throw new InvalidOperationException($"No existe la base de datos de la empresa {tenantId.Value}.");
        effectiveConnection = $"Data Source={tenantPath}";
    }

    if (useSqlite)
        options.UseSqlite(effectiveConnection);
    else
        options.UseSqlServer(effectiveConnection);
});
builder.Services.AddDbContext<MasterDbContext>(options =>
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
 .AddEntityFrameworkStores<MasterDbContext>()
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
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
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
    // Encapsulación de datos por sesión: todo endpoint exige usuario autenticado
    // salvo [AllowAnonymous] explícito (login, forgot/reset-password, onboarding-check).
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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

builder.Services.AddControllers(o => o.Filters.Add<ERP.Api.Infrastructure.BootstrapOnlyOnboardingFilter>()).AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
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
        var masterContext = services.GetRequiredService<MasterDbContext>();
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

        // Reconciliar instalaciones anteriores: antes de UserEmpresas algunos
        // usuarios solo tenían EmpresaId. Crear la pertenencia explícita evita
        // que el aislamiento y el estado del onboarding dependan de ese legado.
        var usersWithPrimaryCompany = await masterContext.Users
            .Where(u => u.EmpresaId.HasValue && u.EmpresaId.Value > 0)
            .Select(u => new { u.Id, EmpresaId = u.EmpresaId!.Value })
            .ToListAsync();
        var existingMemberships = await masterContext.UserEmpresas
            .Select(ue => new { ue.UserId, ue.EmpresaId })
            .ToListAsync();
        var existingMembershipKeys = existingMemberships
            .Select(ue => $"{ue.UserId}:{ue.EmpresaId}")
            .ToHashSet(StringComparer.Ordinal);
        var missingMemberships = usersWithPrimaryCompany
            .Where(u => !existingMembershipKeys.Contains($"{u.Id}:{u.EmpresaId}"))
            .Select(u => new UserEmpresa { UserId = u.Id, EmpresaId = u.EmpresaId })
            .ToList();
        if (missingMemberships.Count > 0)
        {
            masterContext.UserEmpresas.AddRange(missingMemberships);
            await masterContext.SaveChangesAsync();
        }

        await SeedService.SeedAsync(context);

// Toda empresa existente debe tener su almacén físico antes de que
        // pueda emitirse un token con su EmpresaId. La operación es idempotente:
        // una empresa ya provisionada solo aplica las migraciones pendientes.
        var tenantProvisioner = services.GetRequiredService<TenantDatabaseProvisioner>();
        var empresasExistentes = await context.Empresas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();
        foreach (var empresaExistente in empresasExistentes)
            await tenantProvisioner.EnsureCreatedAsync(empresaExistente);

        // --- 8b. SEED BOOTSTRAP: usuario genérico ÚNICO de primera interacción ---
        // Un solo usuario (admin@erp.local), SIN rol de administrador y SIN permisos.
        // Lo único que puede hacer es el primer onboarding: crear la empresa
        // y el primer usuario asociado a esa empresa.
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        // Limpieza del alias legacy: solo existe admin@erp.local
        var bootstrapCom = await userManager.FindByEmailAsync("admin@erp.com");
        if (bootstrapCom != null)
        {
            await userManager.DeleteAsync(bootstrapCom);
            services.GetRequiredService<ILogger<Program>>().LogInformation("Alias legacy admin@erp.com eliminado: el usuario inicial unificado es admin@erp.local.");
        }

        var bootstrapEmail = ERP.Domain.Constants.BootstrapUser.Email;
>>>>>>> developer

        var bootstrapEmail = ERP.Domain.Constants.BootstrapUser.Email;
        var bootstrap = await userManager.FindByEmailAsync(bootstrapEmail);
        if (bootstrap == null)
        {
            bootstrap = new ApplicationUser
            {
                UserName = bootstrapEmail,
                Email = bootstrapEmail,
                FullName = ERP.Domain.Constants.BootstrapUser.DisplayName,
                EmpresaId = null, // bootstrap sin empresa; la creará tras el primer login
                IsActivo = true,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(bootstrap, ERP.Domain.Constants.BootstrapUser.DefaultPassword);
            if (!createResult.Succeeded)
            {
                var loggerSeed = services.GetRequiredService<ILogger<Program>>();
                loggerSeed.LogError("No se pudo crear usuario bootstrap: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
        if (bootstrap != null)
        {
            // El usuario inicial NUNCA tiene rol Admin ni permisos: solo onboarding.
            if (await userManager.IsInRoleAsync(bootstrap, "Admin"))
                await userManager.RemoveFromRoleAsync(bootstrap, "Admin");
            var bootstrapClaims = await userManager.GetClaimsAsync(bootstrap);
            foreach (var c in bootstrapClaims.Where(c => c.Type == "Permission" || c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role").ToList())
                await userManager.RemoveClaimAsync(bootstrap, c);
        }

        // El bootstrap no debe conservar privilegios aunque proceda de una base
        // creada por una versión anterior que lo sembraba como Admin.
        if (bootstrap != null)
        {
            var oldRoles = await userManager.GetRolesAsync(bootstrap);
            if (oldRoles.Count > 0) await userManager.RemoveFromRolesAsync(bootstrap, oldRoles);
            var oldClaims = await userManager.GetClaimsAsync(bootstrap);
            foreach (var oldClaim in oldClaims)
                await userManager.RemoveClaimAsync(bootstrap, oldClaim);
            var staleLinks = await masterContext.UserEmpresas.Where(x => x.UserId == bootstrap.Id).ToListAsync();
            if (staleLinks.Count > 0)
            {
                masterContext.UserEmpresas.RemoveRange(staleLinks);
                await masterContext.SaveChangesAsync();
            }
        }

        // Bootstrap genérico nunca debe quedar asignado a una empresa (vacío por diseño, RGPD)
        // Si por una asignación previa quedó con EmpresaId, lo limpiamos para que no vea datos de ninguna empresa
        if (bootstrap != null && bootstrap.EmpresaId != null)
        {
            bootstrap.EmpresaId = null;
            bootstrap.SetupTutorialVisto = false;
            bootstrap.SetupTutorialCompletado = false;
            await userManager.UpdateAsync(bootstrap);
        }
        // El alias legacy admin@erp.com se elimina: el usuario inicial unificado es admin@erp.local
        var bootstrapCom = await userManager.FindByEmailAsync("admin@erp.com");
        if (bootstrapCom != null)
        {
            var staleAliasLinks = await masterContext.UserEmpresas.Where(x => x.UserId == bootstrapCom.Id).ToListAsync();
            if (staleAliasLinks.Count > 0)
            {
                masterContext.UserEmpresas.RemoveRange(staleAliasLinks);
                await masterContext.SaveChangesAsync();
            }
            await userManager.DeleteAsync(bootstrapCom);
        }

        // Bootstrap genérico nunca debe quedar asignado a una empresa (vacío por diseño, RGPD)
        // Si por una asignación previa quedó con EmpresaId, lo limpiamos para que no vea datos de ninguna empresa.
        // Tampoco tiene rol Admin ni permisos: solo onboarding.
        if (bootstrap != null)
        {
            if (bootstrap.EmpresaId != null)
            {
                bootstrap.EmpresaId = null;
                bootstrap.SetupTutorialVisto = false;
                bootstrap.SetupTutorialCompletado = false;
                await userManager.UpdateAsync(bootstrap);
            }
            if (await userManager.IsInRoleAsync(bootstrap, "Admin"))
                await userManager.RemoveFromRoleAsync(bootstrap, "Admin");
            var bootstrapClaims = await userManager.GetClaimsAsync(bootstrap);
            foreach (var c in bootstrapClaims.Where(c => c.Type == "Permission" || c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role").ToList())
                await userManager.RemoveClaimAsync(bootstrap, c);
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
