using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Infrastructure;

public sealed class TenantDatabaseProvisioner
{
    private readonly IConfiguration _configuration;
    private readonly ITenantDatabasePathResolver _paths;
    private readonly ILogger<TenantDatabaseProvisioner> _logger;

    public TenantDatabaseProvisioner(
        IConfiguration configuration,
        ITenantDatabasePathResolver paths,
        ILogger<TenantDatabaseProvisioner> logger)
    {
        _configuration = configuration;
        _paths = paths;
        _logger = logger;
    }

    public async Task<string> EnsureCreatedAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        if (empresa.Id <= 0)
            throw new InvalidOperationException("La empresa debe estar guardada antes de crear su base de datos.");

        var path = _paths.GetPath(empresa.Id);
        if (!File.Exists(path) && TryGetMasterSqlitePath(out var masterPath) && File.Exists(masterPath))
        {
            await CloneMasterDatabaseAsync(masterPath, path, cancellationToken);
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        ConfigureProvider(options, $"Data Source={path}");

        await using var tenant = new ApplicationDbContext(options.Options);
        try
        {
            await tenant.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception migrationException)
        {
            _logger.LogWarning(migrationException,
                "No se pudieron aplicar migraciones al tenant {EmpresaId}; se intenta crear el esquema.", empresa.Id);
            await tenant.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (_configuration.GetValue("Database:UseSqlite", true))
            await PartitionClonedDatabaseAsync(path, empresa.Id, cancellationToken);

        var stored = await tenant.Empresas.IgnoreQueryFilters()
            .SingleOrDefaultAsync(e => e.Id == empresa.Id, cancellationToken);
        if (stored == null)
        {
            tenant.Empresas.Add(CloneEmpresa(empresa));
            await tenant.SaveChangesAsync(cancellationToken);
        }

        return path;
    }

    public void DeleteProvisionedDatabase(int empresaId)
    {
        var path = _paths.GetPath(empresaId);
        if (File.Exists(path)) File.Delete(path);
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = path + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
    }

    private void ConfigureProvider(DbContextOptionsBuilder<ApplicationDbContext> options, string sqliteConnection)
    {
        var useSqlite = _configuration.GetValue("Database:UseSqlite", true);
        if (useSqlite)
            options.UseSqlite(sqliteConnection);
        else
            options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")!);
    }

    private bool TryGetMasterSqlitePath(out string path)
    {
        path = string.Empty;
        if (!_configuration.GetValue("Database:UseSqlite", true)) return false;
        var connection = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection)) return false;
        foreach (var part in connection.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length == 2 && pieces[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                path = pieces[1].Trim();
                return !string.IsNullOrWhiteSpace(path);
            }
        }
        return false;
    }

    private static async Task CloneMasterDatabaseAsync(string masterPath, string targetPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (File.Exists(targetPath)) File.Delete(targetPath);

        await using var master = new SqliteConnection($"Data Source={masterPath}");
        await master.OpenAsync(cancellationToken);
        await using var command = master.CreateCommand();
        command.CommandText = "VACUUM INTO $target";
        command.Parameters.AddWithValue("$target", targetPath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task PartitionClonedDatabaseAsync(string path, int empresaId, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(cancellationToken);
        await ExecuteAsync(connection, "PRAGMA foreign_keys = OFF;", cancellationToken);

        var tables = await ReadSingleColumnAsync(connection,
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name NOT IN ('__EFMigrationsHistory', '__EFMigrationsLock');",
            cancellationToken);

        var identityTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AspNetUsers", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins",
            "AspNetUserRoles", "AspNetUserTokens", "AspNetRoleClaims", "UserEmpresas"
        };

        foreach (var table in tables.Where(t => identityTables.Contains(t)))
            await ExecuteAsync(connection, $"DELETE FROM {Quote(table)};", cancellationToken);

        foreach (var table in tables)
        {
            if (identityTables.Contains(table)) continue;
            var columns = await ReadTableColumnsAsync(connection, table, cancellationToken);
            if (table.Equals("Empresas", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteAsync(connection, $"DELETE FROM {Quote(table)} WHERE \"Id\" <> $empresaId;",
                    cancellationToken, ("$empresaId", empresaId));
            }
            else if (columns.Contains("EmpresaId", StringComparer.OrdinalIgnoreCase))
            {
                await ExecuteAsync(connection, $"DELETE FROM {Quote(table)} WHERE \"EmpresaId\" IS NULL OR \"EmpresaId\" <> $empresaId;",
                    cancellationToken, ("$empresaId", empresaId));
            }
        }

        // Las tablas dependientes sin EmpresaId (líneas de documentos,
        // apuntes, movimientos, etc.) se limpian por sus claves foráneas.
        for (var pass = 0; pass < 3; pass++)
        {
            foreach (var child in tables)
            {
                if (identityTables.Contains(child)) continue;
                var foreignKeys = await ReadForeignKeysAsync(connection, child, cancellationToken);
                foreach (var foreignKey in foreignKeys)
                {
                    if (foreignKey.ChildColumns.Count != 1 || foreignKey.ParentColumns.Count != 1) continue;
                    var childColumn = Quote(foreignKey.ChildColumns[0]);
                    var parentColumn = Quote(foreignKey.ParentColumns[0]);
                    await ExecuteAsync(connection,
                        $"DELETE FROM {Quote(child)} WHERE {childColumn} IS NOT NULL AND NOT EXISTS (SELECT 1 FROM {Quote(foreignKey.ParentTable)} p WHERE p.{parentColumn} = {Quote(child)}.{childColumn});",
                        cancellationToken);
                }
            }
        }

        await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
    }

    private static async Task<List<string>> ReadSingleColumnAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        var values = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) values.Add(reader.GetString(0));
        return values;
    }

    private static async Task<HashSet<string>> ReadTableColumnsAsync(SqliteConnection connection, string table, CancellationToken cancellationToken)
    {
        // table_info devuelve cid, name, type...; consultar específicamente la segunda columna.
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({Quote(table)});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(1));
        return result;
    }

    private sealed record ForeignKeyInfo(string ParentTable, List<string> ChildColumns, List<string> ParentColumns);

    private static async Task<List<ForeignKeyInfo>> ReadForeignKeysAsync(SqliteConnection connection, string table, CancellationToken cancellationToken)
    {
        var grouped = new Dictionary<long, ForeignKeyInfo>();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list({Quote(table)});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            var parent = reader.GetString(2);
            var childColumn = reader.GetString(3);
            var parentColumn = reader.GetString(4);
            if (!grouped.TryGetValue(id, out var info))
                grouped[id] = info = new ForeignKeyInfo(parent, new List<string>(), new List<string>());
            info.ChildColumns.Add(childColumn);
            info.ParentColumns.Add(parentColumn);
        }
        return grouped.Values.ToList();
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private static Empresa CloneEmpresa(Empresa source) => new()
    {
        Id = source.Id,
        NombreComercial = source.NombreComercial,
        RazonSocial = source.RazonSocial,
        CIF = source.CIF,
        Direccion = source.Direccion,
        CodigoPostal = source.CodigoPostal,
        Poblacion = source.Poblacion,
        Provincia = source.Provincia,
        Email = source.Email,
        Telefono = source.Telefono,
        Web = source.Web,
        RegistroMercantil = source.RegistroMercantil,
        ColorHex = source.ColorHex,
        Eslogan = source.Eslogan,
        SerieFacturacion = source.SerieFacturacion,
        IvaDefecto = source.IvaDefecto,
        IsActiva = source.IsActiva,
        ModalidadVerifactu = source.ModalidadVerifactu,
        FechaAltaVerifactu = source.FechaAltaVerifactu,
        NombreSistemaInformatico = source.NombreSistemaInformatico,
        VersionSistemaInformatico = source.VersionSistemaInformatico,
        IdSistemaInformatico = source.IdSistemaInformatico,
        NumeroInstalacion = source.NumeroInstalacion,
        TerritorioFiscal = source.TerritorioFiscal,
        EsSII = source.EsSII,
        FechaAlta = source.FechaAlta,
        UltimaModificacion = source.UltimaModificacion
    };
}
