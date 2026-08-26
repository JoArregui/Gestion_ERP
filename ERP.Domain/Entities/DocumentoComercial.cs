using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class DocumentoComercial
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        public TipoDocumento Tipo { get; set; }

        public bool EsCompra { get; set; } = false;

        [Required(ErrorMessage = "El número de documento es obligatorio")]
        [StringLength(50)]
        public string NumeroDocumento { get; set; } = string.Empty; 

        [StringLength(100)]
        public string? NumeroAlbaran { get; set; }

        public string? NumeroFacturaProveedor { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public DateTime? FechaRecepcion { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        
        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        public int? ClienteId { get; set; }
        
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        public int? ProveedorId { get; set; }
        
        [ForeignKey("ProveedorId")]
        public virtual Proveedor? Proveedor { get; set; }

        public int? DocumentoOrigenId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal BaseImponible { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalIva { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Total { get; set; }

        public bool IsContabilizado { get; set; } = false;

        // --- ESTADO SEMÁNTICO PARA USUARIO ---
        public EstadoDocumento Estado { get; set; } = EstadoDocumento.Borrador;

        public string MetodoPago { get; set; } = "Efectivo";

        public string? Observaciones { get; set; } 

        // --- PROPIEDAD PARA EL CIERRE ---
        [StringLength(100)]
        public string? UsuarioNombre { get; set; }

        // --- PROPIEDAD REQUERIDA POR EL COMPONENTE (Error CS1061) ---
        public string? NotasInternas { get; set; }

        // Cambiamos a List para permitir la instanciación directa en el componente (Error CS0144)
        public virtual List<DocumentoLinea> Lineas { get; set; } = new List<DocumentoLinea>();
    }
}