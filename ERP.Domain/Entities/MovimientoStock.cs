using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class MovimientoStock
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public int ArticuloId { get; set; }

        [ForeignKey("ArticuloId")]
        public virtual Articulo? Articulo { get; set; }

        // ESTA ES LA LÍNEA QUE CORRIGE EL ERROR CS0117
        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoMovimiento { get; set; } = string.Empty; 

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal StockResultante { get; set; }

        [StringLength(100)]
        public string ReferenciaDocumento { get; set; } = string.Empty; 

        public string Observaciones { get; set; } = string.Empty;
    }
}