namespace ERP.Domain.DTOs
{
    public class DashboardDetalleDTO
    {
        public string Titulo { get; set; } = string.Empty;
        public List<ItemDetalle> Items { get; set; } = new();
    }

    public class ItemDetalle
    {
        public int IdRelacionado { get; set; }
        public string Principal { get; set; } = string.Empty;
        public string Secundario { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoEnlace { get; set; } = string.Empty; 
    }

    public class EnvioReporteDTO
    {
        public string Titulo { get; set; } = string.Empty;
        public string Destinatario { get; set; } = string.Empty;
        public List<ItemDetalle> Items { get; set; } = new();
    }
}