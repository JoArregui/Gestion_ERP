namespace ERP.Api.Infrastructure;

public interface ITenantDatabasePathResolver
{
    string GetPath(int empresaId);
}
