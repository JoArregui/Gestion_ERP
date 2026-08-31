using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ERP.Domain.Entities.Fiscal; // <-- añadir este using

namespace ERP.Domain.Entities
{
    public class DocumentoLinea
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentoId { get; set; }
        
        [JsonIgnore] // <-- añadir esto
        [ForeignKey("DocumentoId")]
        public virtual DocumentoComercial? Documento { get; set; }

        public int? ArticuloId { get; set; }
        
        [ForeignKey("ArticuloId")]
        public virtual Articulo? Articulo { get; set; }

        [Required]
        public string DescripcionArticulo { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeIva { get; set; }

        // Recargo equivalencia (art. 161 LIVA) — 5.2/1.4/0.5/1.75
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeRecargoEquivalencia { get; set; } = 0m;

        // Retención IRPF profesional (Ley 35/2006: 15% general, 7% inicio, 1% módulos)
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeRetencionIRPF { get; set; } = 0m;

        // Para IGIC/IPSI: tipo puede ser 7,3,0,9.5 etc. — PorcentajeIva se reutiliza; Territorio lo define Empresa
        public TipoIVA TipoIvaCatalogo { get; set; } = TipoIVA.General;

        [NotMapped]
        public decimal Subtotal => Cantidad * PrecioUnitario;

        [NotMapped]
        public decimal ImporteRecargo => Subtotal * PorcentajeRecargoEquivalencia / 100m;

        [NotMapped]
        public decimal ImporteRetencion => Subtotal * PorcentajeRetencionIRPF / 100m;

        [StringLength(100)]
        public string? CategoriaNombre { get; set; }
    }
}