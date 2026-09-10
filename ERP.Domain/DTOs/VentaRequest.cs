namespace ERP.Domain.DTOs
{
    public class VentaRequest
    {
        public int ClienteId { get; set; }
        public List<LineaVentaDTO> Lineas { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class LineaVentaDTO
    {
        public int ArticuloId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}