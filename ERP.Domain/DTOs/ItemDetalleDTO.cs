namespace ERP.Domain.DTOs
{
    public class ItemDetalleDTO
    {
        public string Principal { get; set; } = string.Empty;   // Nombre del Producto o Cliente
        public string Secundario { get; set; } = string.Empty;  // Fecha o Categoría
        public string Referencia { get; set; } = string.Empty;  // SKU o Nº Factura
        public string Valor { get; set; } = string.Empty;       // Cantidad o Importe formateado
    }
}