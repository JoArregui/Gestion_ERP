using System.Collections.Generic;

namespace ERP.Domain.DTOs.Compras
{
    public sealed record PrecioOferta(int ProveedorId, string Proveedor, decimal Precio);

    public sealed record LineaComparativa(int ArticuloId, string Descripcion, decimal CantidadPedida, List<PrecioOferta> Precios);

    public sealed record TotalOferta(int OfertaId, int ProveedorId, string Proveedor, decimal Total, int? PlazoDias, string Estado);

    public sealed record ComparativaRFQ(int RfqId, string Codigo, string Estado, List<LineaComparativa> Lineas, List<TotalOferta> Totales);
}
