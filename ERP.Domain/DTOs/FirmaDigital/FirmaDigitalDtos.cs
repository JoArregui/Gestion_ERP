using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.DTOs.FirmaDigital
{
    // ── Certificado ────────────────────────────────────────────────────
    public class CrearCertificadoDto
    {
        [Required, StringLength(100)] public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Avanzada"; // Simple|Avanzada|Cualificada
        [Required, StringLength(200)] public string SubjectDN { get; set; } = string.Empty;
        [Required, StringLength(200)] public string IssuerDN { get; set; } = string.Empty;
        [Required] public DateTime NotBefore { get; set; }
        [Required] public DateTime NotAfter { get; set; }
        [Required, StringLength(40)] public string SerialNumber { get; set; } = string.Empty;
        [Required, StringLength(64)] public string ThumbprintSHA256 { get; set; } = string.Empty;
        [Required] public string PublicKeyPem { get; set; } = string.Empty;
        public string Uso { get; set; } = "General";
        public string Almacenamiento { get; set; } = "HSM";
        [StringLength(100)] public string? QTSP { get; set; }
        public string? CadenaCertificadosPem { get; set; }
    }

    public class CertificadoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string SubjectDN { get; set; } = string.Empty;
        public string IssuerDN { get; set; } = string.Empty;
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string ThumbprintSHA256 { get; set; } = string.Empty;
        public bool EstaVigente { get; set; }
        public int DiasParaExpiracion { get; set; }
        public bool EstaProximoExpirar { get; set; }
        public string? QTSP { get; set; }
        public bool Revocado { get; set; }
    }

    public class RevocarCertificadoDto
    {
        [Required] public string Motivo { get; set; } = string.Empty;
    }

    // ── Firma ──────────────────────────────────────────────────────────
    public class CrearFirmaDto
    {
        [Required] public int CertificadoId { get; set; }
        public string Tipo { get; set; } = "Avanzada";
        public string Formato { get; set; } = "PAdES"; // PAdES|XAdES|CAdES|JAdES
        [Required, StringLength(100)] public string DocumentoTipo { get; set; } = string.Empty;
        [Required] public int DocumentoId { get; set; }
        [StringLength(64)] public string? HashDocumentoSHA256 { get; set; }
        [Required, StringLength(200)] public string FirmanteNombre { get; set; } = string.Empty;
        [Required, StringLength(20)] public string FirmanteNIF { get; set; } = string.Empty;
        [StringLength(100)] public string? FirmanteCargo { get; set; }
        [StringLength(200)] public string? TSPUrl { get; set; }
    }

    public class FirmaDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Formato { get; set; } = string.Empty;
        public string DocumentoTipo { get; set; } = string.Empty;
        public int DocumentoId { get; set; }
        public string HashDocumentoSHA256 { get; set; } = string.Empty;
        public string FirmanteNombre { get; set; } = string.Empty;
        public string FirmanteNIF { get; set; } = string.Empty;
        public string EstadoVerificacion { get; set; } = string.Empty;
        public string? DetalleVerificacion { get; set; }
        public bool EsValidaLegal { get; set; }
        public DateTime FechaFirma { get; set; }
    }

    // ── Solicitud firma ────────────────────────────────────────────────
    public class CrearSolicitudFirmaDto
    {
        [Required, StringLength(100)] public string Referencia { get; set; } = string.Empty;
        [Required, StringLength(200)] public string Titulo { get; set; } = string.Empty;
        [Required] public string DocumentoBase64 { get; set; } = string.Empty;
        [Required, StringLength(100)] public string NombreArchivo { get; set; } = string.Empty;
        public string TipoFirmaRequerida { get; set; } = "Avanzada";
        public string FormatoSalida { get; set; } = "PAdES";
        public string FirmantesJson { get; set; } = "[]";
        public DateTime? FechaExpiracion { get; set; }
    }

    // ── Sello tiempo ───────────────────────────────────────────────────
    public class CrearSelloTiempoDto
    {
        [Required, StringLength(64)] public string HashDatosSHA256 { get; set; } = string.Empty;
        [Required, StringLength(200)] public string TSAUrl { get; set; } = string.Empty;
        [Required, StringLength(100)] public string TSAName { get; set; } = string.Empty;
        [StringLength(100)] public string? ReferenciaDocumento { get; set; }
    }

    public class SelloTiempoDto
    {
        public int Id { get; set; }
        public string HashDatosSHA256 { get; set; } = string.Empty;
        public DateTime FechaTimestamp { get; set; }
        public string TSAName { get; set; } = string.Empty;
        public string TSAUrl { get; set; } = string.Empty;
        public string TokenBase64 { get; set; } = string.Empty;
    }

    // ── Comunicación certificada ──────────────────────────────────────
    public class CrearComunicacionDto
    {
        [Required, StringLength(100)] public string Referencia { get; set; } = string.Empty;
        [Required, StringLength(200)] public string Asunto { get; set; } = string.Empty;
        [Required] public string Contenido { get; set; } = string.Empty;
        [Required, StringLength(200)] public string RemitenteNombre { get; set; } = string.Empty;
        [Required, StringLength(100)] public string RemitenteEmail { get; set; } = string.Empty;
        public string DestinatariosJson { get; set; } = "[]";
        public string AdjuntosJson { get; set; } = "[]";
        public bool RequiereAcuseRecibo { get; set; } = true;
    }

    public class ComunicacionDto
    {
        public int Id { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaAcuseRecibo { get; set; }
    }
}
