using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class Vencimiento
    {
        [Key]
        public int Id { get; set; }

        public int? DocumentoId { get; set; }
        [ForeignKey("DocumentoId")]
        public virtual DocumentoComercial? Documento { get; set; }

        [Required]
        public DateTime FechaVencimiento { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Importe { get; set; }

        // Estado del pago: "Pendiente", "Pagado", "Anulado", "Devuelto"
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        public DateTime? FechaPago { get; set; }

        // Para trazabilidad en tesorería
        public string? MetodoPago { get; set; } 

        // --- MULTI-TENANCY ---
        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        // Propiedad de ayuda
        public bool EsCobro => Documento != null && !Documento.EsCompra;
    }
}