namespace ERP.Domain.DTOs
{
    public class AjusteStockDTO
    {
        // Campos para Scanpal / Terminal móvil
        public string CodigoBarras { get; set; } = string.Empty;
        public decimal CantidadReal { get; set; }
        public string TerminalId { get; set; } = "Scanpal-Mobile";

        // Campos para gestión web / Ajuste manual (Para corregir errores CS1061)
        public int ArticuloId { get; set; }
        public decimal NuevoStock { get; set; }
        public string? Motivo { get; set; }
    }
}