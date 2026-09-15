using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.DTOs.Trazabilidad
{
    // ── Lote ───────────────────────────────────────────────────────────
    public class CrearLoteDto
    {
        [Required, StringLength(50)] public string CodigoLote { get; set; } = string.Empty;
        [Required] public int ArticuloId { get; set; }
        public int? ProveedorId { get; set; }
        [StringLength(50)] public string? LoteProveedor { get; set; }
        [StringLength(100)] public string? DocumentoOrigen { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        [Required, Range(0.0001, 999999999)] public decimal CantidadInicial { get; set; }
        public DateTime FechaProduccion { get; set; } = DateTime.Now;
        public DateTime? FechaCaducidad { get; set; }
        public DateTime? FechaConsumoPreferente { get; set; }
        public string? CondicionesAlmacenamiento { get; set; }
        public decimal? TemperaturaMinima { get; set; }
        public decimal? TemperaturaMaxima { get; set; }
        public bool EsPCC { get; set; }
        public string? ParametrosCriticos { get; set; }
    }

    public class LoteDto
    {
        public int Id { get; set; }
        public string CodigoLote { get; set; } = string.Empty;
        public int ArticuloId { get; set; }
        public string? ArticuloDescripcion { get; set; }
        public decimal CantidadInicial { get; set; }
        public decimal CantidadActual { get; set; }
        public decimal CantidadDisponible { get; set; }
        public DateTime FechaProduccion { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public bool EstaProximoCaducar { get; set; }
        public bool EstaCaducado { get; set; }
        public bool EsPCC { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool RequiereAnalisis { get; set; }
    }

    public class LoteTrazabilidadCompletaDto
    {
        public LoteDto Lote { get; set; } = new();
        public List<MovimientoLoteDto> Movimientos { get; set; } = new();
        public List<AlertaDto> Alertas { get; set; } = new();
        public List<RetiradaDto> Retiradas { get; set; } = new();
        public object UnPasoAtras { get; set; } = new { };
        public List<object> UnPasoAdelante { get; set; } = new();
    }

    // ── Movimiento ─────────────────────────────────────────────────────
    public class CrearMovimientoLoteDto
    {
        [Required] public int LoteId { get; set; }
        [Required] public string Tipo { get; set; } = "Recepcion"; // TipoMovimientoLote enum name
        [Required, Range(0.0001, 999999999)] public decimal Cantidad { get; set; }
        public int? ProveedorId { get; set; }
        public int? LoteOrigenId { get; set; }
        public int? ClienteId { get; set; }
        public string? TipoDocumento { get; set; }
        public int? DocumentoId { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Transportista { get; set; }
        public string? TemperaturaTransporte { get; set; }
        [StringLength(100)] public string? Responsable { get; set; }
        public int? LoteResultadoId { get; set; }
        public string? ParametrosControlJson { get; set; }
    }

    public class MovimientoLoteDto
    {
        public int Id { get; set; }
        public int LoteId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Cantidad { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Responsable { get; set; }
    }

    // ── Alerta ─────────────────────────────────────────────────────────
    public class AlertaDto
    {
        public int Id { get; set; }
        public int LoteId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Severidad { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaAlerta { get; set; }
        public bool Leida { get; set; }
        public bool Resuelta { get; set; }
    }

    public class ResolverAlertaDto
    {
        [Required] public string AccionCorrectiva { get; set; } = string.Empty;
    }

    // ── Retirada ───────────────────────────────────────────────────────
    public class CrearRetiradaDto
    {
        [Required] public int LoteId { get; set; }
        [Required] public string Tipo { get; set; } = "Otro";
        [Required, StringLength(200)] public string Motivo { get; set; } = string.Empty;
        [Required] public string Severidad { get; set; } = "ClaseII"; // ClaseI|ClaseII|ClaseIII
        [Range(0, 999999999)] public decimal CantidadAfectada { get; set; }
        public int? ClientesAfectados { get; set; }
    }

    public class RetiradaDto
    {
        public int Id { get; set; }
        public int LoteId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Severidad { get; set; } = string.Empty;
        public DateTime FechaDeteccion { get; set; }
        public string? NumeroExpedienteAESAN { get; set; }
        public string? NumeroNotificacionRASFF { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal CantidadAfectada { get; set; }
        public decimal CantidadRetirada { get; set; }
    }

    public class NotificarAesanDto
    {
        [Required] public string NumeroExpediente { get; set; } = string.Empty;
    }
}
