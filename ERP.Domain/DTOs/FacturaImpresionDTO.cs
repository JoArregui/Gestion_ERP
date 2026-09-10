namespace ERP.Domain.DTOs
{
    public class FacturaImpresionDTO
    {
        public string NumeroFactura { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        
        // Datos Empresa (Emisor)
        public string EmisorNombre { get; set; } = "MI EMPRESA S.L.";
        public string EmisorNif { get; set; } = "B12345678";
        public string EmisorDireccion { get; set; } = "Calle Industrial 10, Polígono Norte";
        
        // Datos Cliente (Receptor)
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteNif { get; set; } = string.Empty;
        public string ClienteDireccion { get; set; } = string.Empty;

        public List<LineaFacturaDTO> Lineas { get; set; } = new();
        
        public decimal BaseImponible => Lineas.Sum(l => l.TotalLinea);
        public decimal TotalIva => BaseImponible * 0.21m; // Simplificado al 21%
        public decimal TotalFactura => BaseImponible + TotalIva;
    }

    public class LineaFacturaDTO
    {
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalLinea => Cantidad * PrecioUnitario;
    }
}