using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // <-- añadir este using

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

        [NotMapped]
        public decimal Subtotal => Cantidad * PrecioUnitario;

        [StringLength(100)]
        public string? CategoriaNombre { get; set; }
    }
}