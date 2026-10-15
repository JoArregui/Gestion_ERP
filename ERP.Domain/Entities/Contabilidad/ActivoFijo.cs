using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Contabilidad
{
    /// <summary>
    /// Activo fijo / inmovilizado (PGC grupos 20-23). Fase A flujo ERP.txt §1.
    /// Amortización lineal con asiento auto 681 → 281.
    /// </summary>
    public class ActivoFijo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(30)]
        public string Codigo { get; set; } = string.Empty; // Ej: AF-2026-0001

        [Required]
        [StringLength(200)]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Cuenta PGC inmovilizado: 206, 211, 213, 216, 217, 218...</summary>
        [Required]
        [StringLength(9)]
        public string CuentaActivoCodigo { get; set; } = "21600";

        /// <summary>Cuenta amortización acumulada: 280x / 281x</summary>
        [Required]
        [StringLength(9)]
        public string CuentaAmortizacionCodigo { get; set; } = "28100";

        /// <summary>Cuenta dotación gasto: 681 / 682</summary>
        [Required]
        [StringLength(9)]
        public string CuentaDotacionCodigo { get; set; } = "68100";

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioAdquisicion { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ValorResidual { get; set; } = 0;

        [Required]
        public DateTime FechaAdquisicion { get; set; } = DateTime.Today;

        [Required]
        public DateTime FechaPuestaMarcha { get; set; } = DateTime.Today;

        /// <summary>Vida útil en años (tabla fiscal). Ej: mobiliario 10, equipos informáticos 4.</summary>
        [Required]
        [Range(1, 100)]
        public int VidaUtilAnios { get; set; } = 4;

        public MetodoAmortizacion Metodo { get; set; } = MetodoAmortizacion.Lineal;

        public EstadoActivoFijo Estado { get; set; } = EstadoActivoFijo.Activo;

        // Origen (factura de compra que lo originó)
        public string? OrigenTipo { get; set; } // "FacturaCompra", "Aportacion"
        public int? OrigenId { get; set; }

        public DateTime? FechaBaja { get; set; }
        [StringLength(500)]
        public string? MotivoBaja { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        public virtual ICollection<LineaAmortizacion> PlanAmortizacion { get; set; } = new List<LineaAmortizacion>();

        [NotMapped]
        public decimal BaseAmortizable => Math.Max(0, PrecioAdquisicion - ValorResidual);

        [NotMapped]
        public decimal DotacionAnual => VidaUtilAnios <= 0 ? 0 : Math.Round(BaseAmortizable / VidaUtilAnios, 4);
    }

    /// <summary>
    /// Línea anual del plan de amortización (una por ejercicio).
    /// </summary>
    public class LineaAmortizacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ActivoFijoId { get; set; }

        [ForeignKey(nameof(ActivoFijoId))]
        public virtual ActivoFijo? ActivoFijo { get; set; }

        [Required]
        public int Ejercicio { get; set; } // Año: 2026, 2027...

        [Required]
        public DateTime FechaDotacion { get; set; } // 31-12 del ejercicio

        [Column(TypeName = "decimal(18,4)")]
        public decimal Dotacion { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal AmortizadoAcumulado { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ValorNetoContable { get; set; }

        public bool Contabilizado { get; set; } = false;
        public int? AsientoContableId { get; set; }
        [ForeignKey(nameof(AsientoContableId))]
        public virtual AsientoContable? Asiento { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public enum MetodoAmortizacion
    {
        Lineal = 0
    }

    public enum EstadoActivoFijo
    {
        Activo = 0,
        TotalmenteAmortizado = 1,
        Baja = 2
    }
}
