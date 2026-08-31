using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Contabilidad
{
    /// <summary>
    /// Asiento contable - Cabecera del asiento (PGC Libro Diario)
    /// Cada asiento tiene N apuntes (líneas) que cuadran: Σ Debe = Σ Haber
    /// </summary>
    public class AsientoContable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(20)]
        public string Serie { get; set; } = string.Empty; // Ej: "D", "A", "B", "CIERRE"

        [Required]
        public int Numero { get; set; } // Correlativo por serie y ejercicio

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [StringLength(500)]
        public string Concepto { get; set; } = string.Empty; // Glosa del asiento

        public TipoAsiento Tipo { get; set; } = TipoAsiento.Normal;

        public EstadoAsiento Estado { get; set; } = EstadoAsiento.Borrador;

        // Totales de control (deben coincidir con suma de apuntes)
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalDebe { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalHaber { get; set; }

        // Referencia origen (factura, nómina, banco, etc.)
        public string? OrigenTipo { get; set; } // "Factura", "Nomina", "Banco", "Ajuste", "Apertura", "Cierre"
        public int? OrigenId { get; set; }

        // Usuario y auditoría
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }

        public DateTime? FechaModificacion { get; set; }

        [StringLength(100)]
        public string? UsuarioContabilizacion { get; set; }

        public DateTime? FechaContabilizacion { get; set; }

        // Navegación
        public virtual ICollection<ApunteContable> Apuntes { get; set; } = new List<ApunteContable>();

        // Propiedades calculadas
        [NotMapped]
        public bool Cuadra => Math.Abs(TotalDebe - TotalHaber) < 0.005m;

        [NotMapped]
        public string ReferenciaCompleta => $"{Serie}-{Numero:D6}";

        [NotMapped]
        public int NumeroApuntes => Apuntes?.Count ?? 0;
    }

    /// <summary>
    /// Apunte contable - Línea de detalle del asiento (PGC Libro Diario)
    /// Cada apunte afecta a una cuenta contable en Debe o Haber
    /// </summary>
    public class ApunteContable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AsientoId { get; set; }

        [ForeignKey(nameof(AsientoId))]
        public virtual AsientoContable? Asiento { get; set; }

        [Required]
        public int Orden { get; set; } // Orden dentro del asiento (1, 2, 3...)

        [Required]
        [StringLength(9)]
        public string CuentaContableCodigo { get; set; } = string.Empty;

        [ForeignKey(nameof(CuentaContableCodigo))]
        public virtual CuentaContable? CuentaContable { get; set; }

        [Required]
        public TipoApunte Tipo { get; set; } // Debe / Haber

        [Column(TypeName = "decimal(18,4)")]
        public decimal Importe { get; set; }

        [StringLength(500)]
        public string? Concepto { get; set; } // Concepto específico de la línea

        // Dimensiones analíticas opcionales
        public int? CentroCosteId { get; set; }
        public int? ProyectoId { get; set; }

        // Referencia a documento origen (factura, albarán, etc.)
        public string? DocumentoReferencia { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    public enum TipoAsiento
    {
        Normal = 0,       // Asiento normal de gestión
        Apertura = 1,     // Asiento de apertura de ejercicio
        Cierre = 2,       // Asiento de cierre de ejercicio
        Ajuste = 3,       // Asiento de ajuste/regularización
        Reclasificacion = 4, // Reclasificación de saldos
        Conciliacion = 5, // Asiento de conciliación bancaria
        Amortizacion = 6, // Asiento de amortización
        Provision = 7,    // Asiento de provisión
        Regularizacion = 8 // Regularización IVA, IRPF, etc.
    }

    public enum EstadoAsiento
    {
        Borrador = 0,      // En edición, no contabilizado
        Contabilizado = 1, // Contabilizado definitivamente
        Anulado = 2,       // Anulado (asiento de anulación generado)
        Pendiente = 3      // Pendiente de revisión/aprobación
    }

    public enum TipoApunte
    {
        Debe = 0,
        Haber = 1
    }
}