using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class CierreCaja
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime FechaCierre { get; set; } = DateTime.Now;

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(50)]
        public string Terminal { get; set; } = "TPV-01";
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalVentasEfectivo { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalVentasTarjeta { get; set; }

        // --- DESGLOSE FISCAL ---
        [Column(TypeName = "decimal(18,4)")]
        public decimal Base21 { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Iva21 { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Base10 { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Iva10 { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Base4 { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal Iva4 { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalIva { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal ImporteRealEnCaja { get; set; }

        [NotMapped] 
        public decimal Descuadre => ImporteRealEnCaja - TotalVentasEfectivo;
        
        public string? Observaciones { get; set; } 

        public bool IsProcesado { get; set; } = false;

        // Nuevos campos para reportes detallados
        public string? DataCategoriasJson { get; set; }
        public string? DataUsuariosJson { get; set; }
    }
}