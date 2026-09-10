namespace ERP.Domain.DTOs
{
    public class EtiquetaArticuloDTO
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public int CantidadAImprimir { get; set; } = 1;
    }
}