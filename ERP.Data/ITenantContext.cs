namespace ERP.Data;

/// <summary>
/// Contexto de tenant resuelto por la capa de entrada. La capa de datos no
/// conoce HTTP ni permite que un DTO establezca la empresa activa.
/// </summary>
public interface ITenantContext
{
    int? EmpresaId { get; }
}
