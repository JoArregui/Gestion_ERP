namespace ERP.Api.Infrastructure;

public sealed class TenantDatabasePathResolver : ITenantDatabasePathResolver
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public TenantDatabasePathResolver(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public string GetPath(int empresaId)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));

        var configuredDirectory = _configuration["Database:TenantsDirectory"];
        var directory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(_environment.ContentRootPath, "tenants")
            : (Path.IsPathRooted(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(_environment.ContentRootPath, configuredDirectory));

        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"empresa-{empresaId}.db");
    }
}
