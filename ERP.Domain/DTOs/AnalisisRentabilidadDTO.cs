namespace ERP.Domain.DTOs
{
    public class AnalisisRentabilidadDTO
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Familia { get; set; } = string.Empty;
        
        public decimal PrecioCompra { get; set; } // Coste Medio
        public decimal PrecioVenta { get; set; }  // PVP actual
        
        public decimal MargenEuros => PrecioVenta - PrecioCompra;
        
        // Evitamos división por cero si el precio de venta es 0
        public decimal MargenPorcentaje => PrecioVenta != 0 
            ? (MargenEuros / PrecioVenta) * 100 
            : 0;
            
        public decimal StockActual { get; set; }
        public decimal ValorInventario => StockActual * PrecioCompra;
    }
}