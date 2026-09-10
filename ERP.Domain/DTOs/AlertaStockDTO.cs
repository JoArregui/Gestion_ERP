namespace ERP.Domain.DTOs
{
    public class AlertaStockDTO
    {
        public int ArticuloId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        
        /// <summary>
        /// Cantidad calculada para reponer el stock hasta un nivel de seguridad (150% del mínimo).
        /// </summary>
        public decimal CantidadAReponer => (StockMinimo * 1.5m) - StockActual;

        /// <summary>
        /// Cantidad sugerida básica.
        /// </summary>
        public decimal CantidadSugerida => StockMinimo - StockActual;

        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = "Sin Proveedor";
        public string EmailProveedor { get; set; } = string.Empty;
    }
}