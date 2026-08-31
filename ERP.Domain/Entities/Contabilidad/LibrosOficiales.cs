using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Contabilidad
{
    /// <summary>
    /// Libro Diario - Registro cronológico de todos los asientos (PGC Art. 25 C. Comercio)
    /// Debe legalizarse telemáticamente en Registro Mercantil (4 meses post-cierre)
    /// </summary>
    public class LibroDiario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int EjercicioId { get; set; }

        [ForeignKey(nameof(EjercicioId))]
        public virtual EjercicioContable? Ejercicio { get; set; }

        [Required]
        public DateTime FechaDesde { get; set; }

        [Required]
        public DateTime FechaHasta { get; set; }

        public EstadoLibro Estado { get; set; } = EstadoLibro.Generado;

        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioGeneracion { get; set; }

        // Legalización telemática
        public DateTime? FechaLegalizacion { get; set; }

        [StringLength(100)]
        public string? NumeroLegalizacion { get; set; } // Número de entrada en Registro Mercantil

        [StringLength(500)]
        public string? HashArchivo { get; set; } // SHA256 del fichero XML/PDF generado

        public DateTime? FechaPresentacionRM { get; set; }

        // Totales del periodo
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalDebe { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalHaber { get; set; }

        public int NumeroAsientos { get; set; }

        public int NumeroApuntes { get; set; }

        // Navegación
        public virtual ICollection<AsientoContable> Asientos { get; set; } = new List<AsientoContable>();
    }

    /// <summary>
    /// Libro Mayor - Saldos por cuenta (agrupación de apuntes por cuenta)
    /// No es obligatorio legalizar, pero fundamental para auditoría y balances
    /// </summary>
    public class LibroMayor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int EjercicioId { get; set; }

        [ForeignKey(nameof(EjercicioId))]
        public virtual EjercicioContable? Ejercicio { get; set; }

        [Required]
        [StringLength(9)]
        public string CuentaContableCodigo { get; set; } = string.Empty;

        [ForeignKey(nameof(CuentaContableCodigo))]
        public virtual CuentaContable? CuentaContable { get; set; }

        // Saldos iniciales (arrastre ejercicio anterior)
        [Column(TypeName = "decimal(18,4)")]
        public decimal SaldoInicialDebe { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SaldoInicialHaber { get; set; }

        // Movimientos del ejercicio
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalDebe { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalHaber { get; set; }

        // Saldos finales
        [Column(TypeName = "decimal(18,4)")]
        public decimal SaldoFinalDebe { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SaldoFinalHaber { get; set; }

        public int NumeroApuntes { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // Propiedades calculadas
        [NotMapped]
        public decimal SaldoInicial => SaldoInicialDebe - SaldoInicialHaber;

        [NotMapped]
        public decimal SaldoFinal => SaldoFinalDebe - SaldoFinalHaber;

        [NotMapped]
        public bool EsDeudora => SaldoFinal >= 0;

        [NotMapped]
        public string NaturalezaSaldo => EsDeudora ? "Deudor" : "Acreedor";
    }

    /// <summary>
    /// Libro de Inventarios y Cuentas Anuales (PGC Art. 25 C. Comercio)
    /// Obligatorio legalizar: Balance inicial, Balances de comprobación trimestrales,
    /// Inventario cierre, Cuentas Anuales (Balance, PyG, ECPN, EFE, Memoria)
    /// </summary>
    public class LibroInventariosCuentasAnuales
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int EjercicioId { get; set; }

        [ForeignKey(nameof(EjercicioId))]
        public virtual EjercicioContable? Ejercicio { get; set; }

        public EstadoLibro Estado { get; set; } = EstadoLibro.Generado;

        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioGeneracion { get; set; }

        // Legalización telemática
        public DateTime? FechaLegalizacion { get; set; }

        [StringLength(100)]
        public string? NumeroLegalizacion { get; set; }

        [StringLength(500)]
        public string? HashArchivo { get; set; }

        public DateTime? FechaPresentacionRM { get; set; }

        // Contenido estructurado (JSON/XML)
        public string? BalanceInicialJson { get; set; } // Balance de apertura

        public string? BalancesComprobacionTrimestralesJson { get; set; } // 4 balances

        public string? InventarioCierreJson { get; set; } // Inventario final

        // Cuentas Anuales
        public string? BalanceSituacionJson { get; set; } // Activo / Pasivo / Patrimonio

        public string? CuentaPerdidasGananciasJson { get; set; } // PyG

        public string? EstadoCambiosPatrimonioNetJson { get; set; } // ECPN

        public string? EstadoFlujosEfectivoJson { get; set; } // EFE (si obligatorio)

        public string? MemoriaJson { get; set; } // Memoria anual

        // Abreviadas (si aplica)
        public bool EsAbreviado { get; set; }
    }

    /// <summary>
    /// Ejercicio contable - Periodo fiscal (normalmente año natural)
    /// </summary>
    public class EjercicioContable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(10)]
        public string Codigo { get; set; } = string.Empty; // Ej: "2026"

        [Required]
        public DateTime FechaInicio { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);

        [Required]
        public DateTime FechaFin { get; set; } = new DateTime(DateTime.Now.Year, 12, 31, 23, 59, 59);

        public EstadoEjercicio Estado { get; set; } = EstadoEjercicio.Abierto;

        public DateTime? FechaCierre { get; set; }

        [StringLength(100)]
        public string? UsuarioCierre { get; set; }

        // Cierres intermedios
        public bool CierreTrimestral1 { get; set; }
        public bool CierreTrimestral2 { get; set; }
        public bool CierreTrimestral3 { get; set; }
        public bool CierreTrimestral4 { get; set; }

        // Legalización libros
        public bool LibrosLegalizados { get; set; }

        public DateTime? FechaLegalizacionLibros { get; set; }

        // Depósito cuentas anuales
        public bool CuentasDepositadas { get; set; }

        public DateTime? FechaDepositoCuentas { get; set; }

        [StringLength(100)]
        public string? NumeroDeposito { get; set; }
    }

    public enum EstadoLibro
    {
        Generado = 0,
        Validado = 1,
        Legalizado = 2,
        Error = 3
    }

    public enum EstadoEjercicio
    {
        Abierto = 0,
        CerradoParcial = 1, // Cierres trimestrales
        Cerrado = 2,        // Cierre definitivo
        Bloqueado = 3       // No permite más asientos
    }
}