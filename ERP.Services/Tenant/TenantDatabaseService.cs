using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Services.Tenant
{
    /// <summary>
    /// Gestiona DB por empresa: GestionX.db por cada empresa X.
    /// Pensado para despliegues on-premise / multi-PC por empresa (miles de ordenadores).
    /// Cada PC apunta a su GestionX.db local; el bootstrap admin@erp.local es universal.
    /// </summary>
    public class TenantDatabaseService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TenantDatabaseService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TenantDatabaseService(IConfiguration config, ILogger<TenantDatabaseService> logger, IServiceProvider serviceProvider)
        {
            _config = config;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public static string SanitizeEmpresa(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "Empresa";
            // Solo alfanumérico, sin espacios, primera letra mayúscula
            var limpio = new string(nombre.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(limpio)) limpio = "Empresa";
            // Capitalizar primera
            return char.ToUpper(limpio[0]) + limpio.Substring(1);
        }

        public static string GetTenantFileName(string nombreEmpresa) => $"Gestion{SanitizeEmpresa(nombreEmpresa)}.db";

        public string GetTenantDbPath(string nombreEmpresa)
        {
            var fileName = GetTenantFileName(nombreEmpresa);
            // Directorio del DB actual (donde está erp.db / erp-fresh.db)
            var currentConn = _config.GetConnectionString("DefaultConnection") ?? "Data Source=erp.db";
            var currentFile = ExtractFileName(currentConn) ?? "erp.db";
            var baseDir = AppContext.BaseDirectory;
            // Si es relativo, resolver respecto a la carpeta del API
            var currentPath = Path.IsPathRooted(currentFile) ? currentFile : Path.Combine(baseDir, currentFile);
            var dir = Path.GetDirectoryName(currentPath) ?? baseDir;
            // Si dir no existe o es temporal, usar ContentRoot del API (donde está el .db real)
            if (!Directory.Exists(dir) || dir.Contains("Temp"))
            {
                // Fallback: carpeta del ensamblado ERP.Data
                var dataDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ERP.Api");
                if (Directory.Exists(dataDir)) dir = Path.GetFullPath(dataDir);
                else dir = baseDir;
            }
            return Path.Combine(dir, fileName);
        }

        private static string? ExtractFileName(string conn)
        {
            // Data Source=xxx
            var parts = conn.Split(';');
            foreach (var p in parts)
            {
                var kv = p.Split('=', 2);
                if (kv.Length == 2 && kv[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return null;
        }

        /// <summary>
        /// Asegura que existe GestionX.db. Si no existe, la crea clonando el esquema y seeds, e inserta la empresa.
        /// Devuelve la ruta del fichero.
        /// </summary>
        public async Task<string> EnsureTenantDatabaseAsync(Empresa empresa)
        {
            var path = GetTenantDbPath(empresa.RazonSocial ?? empresa.NombreComercial);
            if (File.Exists(path))
            {
                _logger.LogInformation("Tenant DB ya existe: {Path}", path);
                // Asegurar que la empresa existe en ese fichero también
                await EnsureEmpresaInTenantAsync(path, empresa);
                return path;
            }

            _logger.LogInformation("Creando tenant DB GestionX: {Path} para empresa {Empresa}", path, empresa.RazonSocial);

            // Crear nuevo DbContext apuntando al nuevo fichero y migrar
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // Reusar proveedor actual (Sqlite vs SqlServer) mirando DefaultConnection
            var useSqliteStr = _config["Database:UseSqlite"];
            var useSqlite = bool.TryParse(useSqliteStr, out var u) ? u : true;
            var tenantConn = useSqlite ? $"Data Source={path}" : _config.GetConnectionString("DefaultConnection")!;

            if (useSqlite)
                optionsBuilder.UseSqlite(tenantConn);
            else
                optionsBuilder.UseSqlServer(tenantConn);

            using var tenantCtx = new ApplicationDbContext(optionsBuilder.Options);
            try
            {
                await tenantCtx.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Migrate falló en tenant, probando EnsureCreated");
                await tenantCtx.Database.EnsureCreatedAsync();
            }

            // Insertar empresa si no existe
            await EnsureEmpresaInTenantAsync(path, empresa, tenantCtx);

            // Crear marker de tenant activo para que el próximo arranque pueda auto-seleccionar
            try
            {
                var markerPath = Path.Combine(Path.GetDirectoryName(path) ?? ".", "tenant.json");
                var json = System.Text.Json.JsonSerializer.Serialize(new { ActiveDatabase = Path.GetFileName(path), Empresa = empresa.RazonSocial, Fecha = DateTime.Now });
                await File.WriteAllTextAsync(markerPath, json);
            }
            catch { }

            return path;
        }

        private async Task EnsureEmpresaInTenantAsync(string path, Empresa empresa, ApplicationDbContext? existingCtx = null)
        {
            ApplicationDbContext ctx = existingCtx!;
            bool ownsCtx = false;
            if (ctx == null)
            {
                var useSqliteStr2 = _config["Database:UseSqlite"];
                var useSqlite2 = bool.TryParse(useSqliteStr2, out var u2) ? u2 : true;
                var tenantConn = useSqlite2 ? $"Data Source={path}" : _config.GetConnectionString("DefaultConnection")!;
                var opts = new DbContextOptionsBuilder<ApplicationDbContext>();
                if (useSqlite2) opts.UseSqlite(tenantConn); else opts.UseSqlServer(tenantConn);
                ctx = new ApplicationDbContext(opts.Options);
                ownsCtx = true;
            }
            try
            {
                var exists = await ctx.Empresas.AnyAsync(e => e.RazonSocial == empresa.RazonSocial || e.CIF == empresa.CIF);
                if (!exists)
                {
                    // Clonar empresa sin Id
                    var clone = new Empresa
                    {
                        NombreComercial = empresa.NombreComercial,
                        RazonSocial = empresa.RazonSocial,
                        CIF = empresa.CIF,
                        SerieFacturacion = empresa.SerieFacturacion,
                        IvaDefecto = empresa.IvaDefecto,
                        IsActiva = true,
                        ColorHex = empresa.ColorHex,
                        FechaAlta = DateTime.Now,
                        TerritorioFiscal = empresa.TerritorioFiscal,
                        EsSII = empresa.EsSII
                    };
                    ctx.Empresas.Add(clone);
                    await ctx.SaveChangesAsync();
                    _logger.LogInformation("Empresa {Razon} insertada en tenant {Path} con Id {Id}", clone.RazonSocial, path, clone.Id);
                }
            }
            finally
            {
                if (ownsCtx) await ctx.DisposeAsync();
            }
        }

        public string[] ListTenantDatabases()
        {
            var dir = Path.GetDirectoryName(GetTenantDbPath("dummy")) ?? AppContext.BaseDirectory;
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir, "Gestion*.db").Select(Path.GetFileName).ToArray()!;
        }
    }
}
