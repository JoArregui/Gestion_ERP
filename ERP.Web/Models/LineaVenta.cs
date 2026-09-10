using ERP.Domain.Entities;

namespace ERP.Web.Models
{
    public class LineaVenta
    {
        public Articulo Articulo { get; set; } = null!;
        public decimal Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        
        // Cálculo automático de línea con IVA incluido
        public decimal Subtotal => Cantidad * PrecioUnitario;
        public decimal ImporteIva => Subtotal * (Articulo.PorcentajeIva / 100);
        public decimal Total => Subtotal + ImporteIva;
    }
}