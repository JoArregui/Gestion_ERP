namespace ERP.Domain.DTOs;

public sealed class PedidoCompraRequest
{
    public int ProveedorId { get; set; }
    public List<LineaPedidoCompraRequest> Lineas { get; set; } = new();
}

public sealed class LineaPedidoCompraRequest
{
    public int ArticuloId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
