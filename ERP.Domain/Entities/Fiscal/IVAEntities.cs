﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Fiscal
{
    /// <summary>
    /// ConfiguraciÃ³n IVA por empresa (prorrata, sectores diferenciados, recargo equivalencia)
    /// Art. 99-108 LIVA + Art. 161 LIVA (recargo equivalencia)
    /// </summary>
    public class ConfiguracionIVA
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
        public virtual Contabilidad.EjercicioContable? Ejercicio { get; set; }

        // --- PRORRATA GENERAL (Art. 102-103 LIVA) ---
        public bool AplicaProrrataGeneral { get; set; } = false;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PorcentajeProrrataGeneral { get; set; } // Calculado: (Base deducible / Base total) * 100

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PorcentajeProrrataGeneralRedondeado { get; set; } // Redondeado al alza (entero superior)

        // --- PRORRATA ESPECIAL (Art. 104-105 LIVA) ---
        public bool AplicaProrrataEspecial { get; set; } = false; // Opcional u obligatoria si general perjudica >20%

        // Detalle prorrata especial por tipo de bien/servicio
        public string? DetalleProrrataEspecialJson { get; set; } // JSON: { "BienesInversion": 100, "BienesCorrientesDeducibles": 100, "BienesCorrientesNoDeducibles": 0, "Mixtos": %prorrata }

        // --- SECTORES DIFERENCIADOS (Art. 9.1.c LIVA) ---
        public bool TieneSectoresDiferenciados { get; set; } = false;

        public string? SectoresJson { get; set; } // JSON: [{"Codigo":"SECT1","Nombre":"Actividad A","PorcentajeDeduccion":100,"VolumenOperaciones":100000},{"Codigo":"SECT2","Nombre":"Actividad B","PorcentajeDeduccion":0,"VolumenOperaciones":50000}]

        // --- RECARGO DE EQUIVALENCIA (Art. 161 LIVA) ---
        public bool SujetoRecargoEquivalencia { get; set; } = false;

        [Column(TypeName = "decimal(5,2)")]
        public decimal RecargoGeneral { get; set; } = 5.2m;    // 5,2% sobre base 21%
        [Column(TypeName = "decimal(5,2)")]
        public decimal RecargoReducido { get; set; } = 1.4m;   // 1,4% sobre base 10%
        [Column(TypeName = "decimal(5,2)")]
        public decimal RecargoSuperreducido { get; set; } = 0.5m; // 0,5% sobre base 4%
        [Column(TypeName = "decimal(5,2)")]
        public decimal RecargoTabaco { get; set; } = 1.75m;    // 1,75% labores tabaco

        // --- REGIMENES ESPECIALES ---
        public bool RegimenAgenciasViajes { get; set; } = false;      // Art. 149-156 LIVA
        public bool RegimenBienesUsados { get; set; } = false;        // Art. 134-138 LIVA
        public bool RegimenObjetosArte { get; set; } = false;         // Art. 139-143 LIVA
        public bool RegimenOroInversion { get; set; } = false;        // Art. 144-148 LIVA
        public bool RegimenServiciosElectronicos { get; set; } = false; // Art. 93-98 LIVA (OSS/IOSS)

        // --- IVA CAJA (Art. 163 bis LIVA) ---
        public bool AplicaIVACaja { get; set; } = false;
        public DateTime? FechaInicioIVACaja { get; set; }
        public DateTime? FechaFinIVACaja { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? LimiteVolumenOperacionesIVACaja { get; set; } // 2.000.000 â‚¬

        // --- OTROS ---
        public bool InversionSujetoPasivoHabitual { get; set; } = false; // Art. 84 LIVA
        public bool AutoliquidacionRectificativaAnual { get; set; } = false;

        // AuditorÃ­a
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }
    }

    /// <summary>
    /// LiquidaciÃ³n IVA periÃ³dica (Modelo 303) - CÃ¡lculo automÃ¡tico con prorrata/sectores
    /// </summary>
    public class LiquidacionIVA
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
        public virtual Contabilidad.EjercicioContable? Ejercicio { get; set; }

        [Required]
        public PeriodoIVA Periodo { get; set; } // 1T, 2T, 3T, 4T, Anual

        [Required]
        public int Año { get; set; }

        public EstadoLiquidacionIVA Estado { get; set; } = EstadoLiquidacionIVA.Borrador;

        // Fechas
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public DateTime? FechaPresentacion { get; set; }
        public DateTime? FechaPago { get; set; }

        // --- BASES IMPONIBLES POR TIPO ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseGeneral { get; set; } // 21%
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseReducida { get; set; } // 10%
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSuperreducida { get; set; } // 4%
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseExenta { get; set; } // Art. 20 LIVA
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseNoSujeta { get; set; } // Exportaciones, intracomunitarias
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseInversionSujetoPasivo { get; set; } // Art. 84 LIVA

        // --- IVA REPERCUTIDO (SOPORTADO EN VENTAS) ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAGeneralRepercutido { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAReducidoRepercutido { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVASuperreducidoRepercutido { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVARecargoEquivalencia { get; set; }

        // --- IVA SOPORTADO (DEDUCIBLE EN COMPRAS) ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAGeneralSoportado { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAReducidoSoportado { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVASuperreducidoSoportado { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVARecargoEquivalenciaSoportado { get; set; }

        // --- PRORRATA / SECTORES ---
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeProrrataAplicada { get; set; } // % final aplicado

        public bool UsaProrrataEspecial { get; set; }
        public string? DetalleProrrataAplicadaJson { get; set; } // CÃ³mo se calculÃ³

        public bool UsaSectoresDiferenciados { get; set; }
        public string? DetalleSectoresJson { get; set; } // DeducciÃ³n por sector

        // --- IVA DEDUCIBLE FINAL (TRAS PRORRATA/SECTORES) ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVADeducibleGeneral { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVADeducibleReducido { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVADeducibleSuperreducido { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVADeducibleTotal { get; set; }

        // --- IVA DEVENGADO / RESULTADO ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVADevengadoTotal { get; set; } // Repercutido - Deducible

        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAAIngresar { get; set; } // Positivo = ingresar, Negativo = devolver/compensar

        [Column(TypeName = "decimal(18,2)")]
        public decimal IVACompensar { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal IVADevolver { get; set; }

        // --- REGIMENES ESPECIALES (detalle) ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAAgenciasViajes { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVABienesUsados { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAObjetosArte { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAOroInversion { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVAServiciosElectronicos { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal IVACaja { get; set; }

        // RegularizaciÃ³n anual (Art. 107 LIVA)
        [Column(TypeName = "decimal(18,2)")]
        public decimal RegularizacionAnual { get; set; }

        // AuditorÃ­a
        public DateTime FechaCalculo { get; set; } = DateTime.Now;
        public string? ReferenciaPresentacion { get; set; } // Número justificante AEAT
        [StringLength(100)]
        public string? UsuarioCalculo { get; set; }
    }

    /// <summary>
    /// Detalle de operaciÃ³n para cÃ¡lculo IVA (lÃ­nea de factura, gasto, intracomunitaria, etc.)
    /// Permite trazabilidad completa del cÃ¡lculo de cada casilla del 303
    /// </summary>
    public class DetalleLiquidacionIVA
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LiquidacionIVAId { get; set; }
        [ForeignKey(nameof(LiquidacionIVAId))]
        public virtual LiquidacionIVA? Liquidacion { get; set; }

        [Required]
        public TipoOperacionIVA TipoOperacion { get; set; } // Venta, Compra, Intracom, Import, Export, Autoconsumo, Regularizacion

        [Required]
        public TipoIVA TipoIVA { get; set; } // General, Reducido, Superreducido, Exento, NoSujeto, RecargoEquiv

        public int? DocumentoId { get; set; } // Factura, AlbarÃ¡n, Gasto, etc.
        public string? TipoDocumento { get; set; } // "FacturaEmitida", "FacturaRecibida", "Albaran", "Gasto"

        [StringLength(50)]
        public string? NumeroDocumento { get; set; }

        public DateTime FechaOperacion { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseImponible { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TipoImpositivo { get; set; } // 21, 10, 4, 0, 5.2, 1.4, 0.5

        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaIVA { get; set; }

        // Para prorrata/sectores
        public int? SectorDiferenciadoId { get; set; }
        public bool EsDeducible { get; set; } = true;
        public decimal? PorcentajeDeduccion { get; set; } // 100, 0, o % prorrata

        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaDeducible { get; set; } // Tras aplicar prorrata/sector

        // InversiÃ³n sujeto pasivo
        public bool InversionSujetoPasivo { get; set; } = false;

        // RegÃ­menes especiales
        public string? RegimenEspecial { get; set; }

        // Referencia contable
        public int? AsientoContableId { get; set; }
    }

    /// <summary>
    /// Sector diferenciado de actividad (Art. 9.1.c LIVA)
    /// Se crea cuando diferencia de % deducciÃ³n > 50 puntos porcentuales
    /// </summary>
    public class SectorDiferenciadoIVA
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty; // "SECT1", "SECT2"...

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeDeduccion { get; set; } // 0-100

        [Column(TypeName = "decimal(18,2)")]
        public decimal VolumenOperacionesAnual { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        // NavegaciÃ³n
        public virtual ICollection<DetalleLiquidacionIVA> DetallesLiquidacion { get; set; } = new List<DetalleLiquidacionIVA>();
    }

    // Enums
    public enum PeriodoIVA
    {
        PrimerTrimestre = 1,
        SegundoTrimestre = 2,
        TercerTrimestre = 3,
        CuartoTrimestre = 4,
        Anual = 5,          // Resumen anual
        Mensual = 6         // SII / Grandes empresas
    }

    public enum EstadoLiquidacionIVA
    {
        Borrador = 0,
        Calculada = 1,
        Validada = 2,
        Presentada = 3,
        Pagada = 4,
        Rectificada = 5,
        Anulada = 6
    }

    public enum TipoOperacionIVA
    {
        VentaNacional = 0,
        VentaIntracomunitaria = 1,
        Exportacion = 2,
        CompraNacional = 3,
        CompraIntracomunitaria = 4,
        Importacion = 5,
        Autoconsumo = 6,
        RegularizacionAnual = 7,
        InversionSujetoPasivo = 8,
        RegimenEspecial = 9,
        IVACaja = 10
    }

    public enum TipoIVA
    {
        General = 0,           // 21%
        Reducido = 1,          // 10%
        Superreducido = 2,     // 4%
        ExentoArt20 = 3,       // Exento Art. 20 LIVA
        ExentoArt21 = 4,       // Exento Art. 21 LIVA (exportaciones)
        ExentoArt22 = 5,       // Exento Art. 22 LIVA (intracom)
        NoSujeto = 6,          // No sujeta (Art. 7 LIVA)
        RecargoEquivalenciaGeneral = 7,   // 5,2%
        RecargoEquivalenciaReducido = 8,  // 1,4%
        RecargoEquivalenciaSuperreducido = 9, // 0,5%
        RecargoEquivalenciaTabaco = 10,   // 1,75%
        InversionSujetoPasivo = 11,
        RegimenAgenciasViajes = 12,
        RegimenBienesUsados = 13,
        RegimenObjetosArte = 14,
        RegimenOroInversion = 15,
        IVACaja = 16
    }
}
