namespace ERP.Domain.DTOs
{
    public class ValoracionStockDTO
    {
        public decimal ValorTotalAlmacen { get; set; }
        public int TotalArticulosDiferentes { get; set; }
        public double CantidadTotalUnidades { get; set; }
        public List<ArticuloValoradoDTO> TopArticulosMasValiosos { get; set; } = new();
    }

    public class ArticuloValoradoDTO
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public double Stock { get; set; }
        public decimal PMP { get; set; }
        public decimal ValorTotal => (decimal)Stock * PMP;
    }
}