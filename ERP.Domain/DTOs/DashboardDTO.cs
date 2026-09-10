using System.Collections.Generic;

namespace ERP.Domain.DTOs
{
    public class DashboardDTO
    {
        public decimal TotalVentas { get; set; }
        public decimal TotalCompras { get; set; }
        public decimal TotalNominas { get; set; }
        public decimal BeneficioNeto { get; set; } 

        public int FacturasPendientesCobro { get; set; }
        public decimal ImportePendienteCobro { get; set; }
        public int FacturasVencidas { get; set; }
        public int ArticulosStockBajo { get; set; }
        public List<GraficoVentasMes> VentasMensuales { get; set; } = new();
    }

    public class GraficoVentasMes
    {
        public string Mes { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public int Orden { get; set; }
    }
}