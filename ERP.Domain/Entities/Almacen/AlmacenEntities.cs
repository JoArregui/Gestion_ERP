using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Almacen
{
    /// <summary>
    /// Almacén físico / nave (Fase D flujo ERP.txt §4). Multialmacén por empresa.
    /// </summary>
    public class Almacen
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty; // ALM-01

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Direccion { get; set; }

        public bool EsPrincipal { get; set; } = false;
        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
    }

    /// <summary>
    /// Ubicación pasillo / estantería / nivel dentro de un almacén.
    /// </summary>
    public class Ubicacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlmacenId { get; set; }

        [ForeignKey(nameof(AlmacenId))]
        public virtual Almacen? Almacen { get; set; }

        [Required]
        [StringLength(30)]
        public string Codigo { get; set; } = string.Empty; // A-01-03

        [Required]
        [StringLength(10)]
        public string Pasillo { get; set; } = "A";

        [Required]
        [StringLength(10)]
        public string Estanteria { get; set; } = "01";

        [Required]
        [StringLength(10)]
        public string Nivel { get; set; } = "01";

        public bool Activa { get; set; } = true;
    }

    /// <summary>
    /// Desglose de stock por ubicación. Articulo.Stock sigue siendo el total físico;
    /// "sin ubicar" = Stock - StockReservado - Σ ubicaciones.
    /// </summary>
    public class StockUbicacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int AlmacenId { get; set; }

        [ForeignKey(nameof(AlmacenId))]
        public virtual Almacen? Almacen { get; set; }

        [Required]
        public int UbicacionId { get; set; }

        [ForeignKey(nameof(UbicacionId))]
        public virtual Ubicacion? Ubicacion { get; set; }

        [Required]
        public int ArticuloId { get; set; }

        [ForeignKey(nameof(ArticuloId))]
        public virtual Articulo? Articulo { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        /// <summary>Reservado por albaranes a nivel de ubicación (paralelo a Articulo.StockReservado).</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal Reservado { get; set; }

        public DateTime UltimoMovimiento { get; set; } = DateTime.Now;
    }
}
