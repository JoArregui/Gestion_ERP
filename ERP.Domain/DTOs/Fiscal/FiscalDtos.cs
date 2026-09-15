using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.DTOs.Fiscal
{
    // ── Configuración IVA (prorrata, sectores, recargo, caja) ─────────
    public class CrearConfiguracionIVADto
    {
        [Required] public int EjercicioId { get; set; }
        public bool AplicaProrrataGeneral { get; set; }
        [Range(0, 100)] public decimal? PorcentajeProrrataGeneral { get; set; }
        public bool AplicaProrrataEspecial { get; set; }
        public string? DetalleProrrataEspecialJson { get; set; }
        public bool TieneSectoresDiferenciados { get; set; }
        public string? SectoresJson { get; set; }
        public bool SujetoRecargoEquivalencia { get; set; }
        [Range(0, 20)] public decimal RecargoGeneral { get; set; } = 5.2m;
        [Range(0, 20)] public decimal RecargoReducido { get; set; } = 1.4m;
        [Range(0, 20)] public decimal RecargoSuperreducido { get; set; } = 0.5m;
        [Range(0, 20)] public decimal RecargoTabaco { get; set; } = 1.75m;
        public bool RegimenAgenciasViajes { get; set; }
        public bool RegimenBienesUsados { get; set; }
        public bool RegimenObjetosArte { get; set; }
        public bool RegimenOroInversion { get; set; }
        public bool AplicaIVACaja { get; set; }
        public bool InversionSujetoPasivoHabitual { get; set; }
    }

    public class ConfiguracionIVADto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int EjercicioId { get; set; }
        public bool AplicaProrrataGeneral { get; set; }
        public decimal? PorcentajeProrrataGeneral { get; set; }
        public decimal? PorcentajeProrrataGeneralRedondeado { get; set; }
        public bool AplicaProrrataEspecial { get; set; }
        public bool TieneSectoresDiferenciados { get; set; }
        public bool SujetoRecargoEquivalencia { get; set; }
        public decimal RecargoGeneral { get; set; }
        public bool AplicaIVACaja { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    // ── Tarifas IGIC/IPSI/IVA ─────────────────────────────────────────
    public class CrearTarifaImpuestoDto
    {
        [Required] public string Territorio { get; set; } = "PeninsulaBaleares"; // enum name
        [Required] public string TipoIVA { get; set; } = "General";
        [Required, StringLength(50)] public string Nombre { get; set; } = string.Empty;
        [Required, Range(0, 100)] public decimal Porcentaje { get; set; }
        [Range(0, 20)] public decimal RecargoEquivalencia { get; set; }
        public bool Vigente { get; set; } = true;
        public DateTime FechaDesde { get; set; } = new DateTime(2026, 1, 1);
        public DateTime? FechaHasta { get; set; }
    }

    public class TarifaImpuestoDto
    {
        public int Id { get; set; }
        public string Territorio { get; set; } = string.Empty;
        public string TipoIVA { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
        public decimal RecargoEquivalencia { get; set; }
        public bool Vigente { get; set; }
    }

    // ── Sectores diferenciados ────────────────────────────────────────
    public class CrearSectorDiferenciadoDto
    {
        [Required, StringLength(20)] public string Codigo { get; set; } = string.Empty;
        [Required, StringLength(100)] public string Nombre { get; set; } = string.Empty;
        [StringLength(500)] public string? Descripcion { get; set; }
        [Required, Range(0, 100)] public decimal PorcentajeDeduccion { get; set; }
        [Range(0, 999999999999)] public decimal VolumenOperacionesAnual { get; set; }
    }

    // ── Liquidación IVA 303 ───────────────────────────────────────────
    public class CalcularLiquidacionDto
    {
        [Required] public int EjercicioId { get; set; }
        [Required] public string Periodo { get; set; } = "PrimerTrimestre"; // enum name
        [Required, Range(2020, 2100)] public int Año { get; set; }
    }

    public class LiquidacionIVADto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public int Año { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public decimal BaseGeneral { get; set; }
        public decimal BaseReducida { get; set; }
        public decimal BaseSuperreducida { get; set; }
        public decimal IVAGeneralRepercutido { get; set; }
        public decimal IVADeducibleTotal { get; set; }
        public decimal IVADevengadoTotal { get; set; }
        public decimal IVAAIngresar { get; set; }
        public decimal IVADevolver { get; set; }
        public decimal PorcentajeProrrataAplicada { get; set; }
        public bool Presentado { get; set; }
        public string? ReferenciaPresentacion { get; set; }
        public DateTime FechaCalculo { get; set; }
    }

    public class PresentarLiquidacionDto
    {
        [Required] public string ReferenciaAEAT { get; set; } = string.Empty;
    }

    public class DetalleLiquidacionDto
    {
        public int Id { get; set; }
        public string TipoOperacion { get; set; } = string.Empty;
        public string TipoIVA { get; set; } = string.Empty;
        public string? NumeroDocumento { get; set; }
        public DateTime FechaOperacion { get; set; }
        public decimal BaseImponible { get; set; }
        public decimal TipoImpositivo { get; set; }
        public decimal CuotaIVA { get; set; }
        public decimal CuotaDeducible { get; set; }
        public bool EsDeducible { get; set; }
    }

    // ── Libro Registro IVA ────────────────────────────────────────────
    public class LibroRegistroIVADto
    {
        public int Id { get; set; }
        public string TipoLibro { get; set; } = string.Empty;
        public DateTime FechaOperacion { get; set; }
        public string? NumeroFactura { get; set; }
        public string? NIFContraparte { get; set; }
        public decimal BaseImponible { get; set; }
        public decimal TipoImpositivo { get; set; }
        public decimal CuotaIVA { get; set; }
        public decimal CuotaRecargo { get; set; }
        public bool EsDeducible { get; set; }
    }
}
