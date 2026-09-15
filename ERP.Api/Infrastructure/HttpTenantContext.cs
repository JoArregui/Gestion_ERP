using System.Security.Claims;
using ERP.Data;

namespace ERP.Api.Infrastructure;

public sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? EmpresaId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User?.FindFirst("EmpresaId")?.Value;
            return int.TryParse(raw, out var id) && id > 0 ? id : null;
        }
    }
}
