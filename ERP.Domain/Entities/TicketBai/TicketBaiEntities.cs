using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.TicketBai
{
    /// <summary>
    /// Registro de factura para TicketBAI (País Vasco - Haciendas Forales: Bizkaia, Gipuzkoa, Álava).
    /// Cumple con la normativa de facturación verificable del País Vasco.
    /// </summary>
    public class TicketBaiRegistro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required, StringLength(30)]
        public string Codigo { get; set; } = string.Empty; // TBI-2026-00001

        [Required]
        public int DocumentoId { get; set; } // Referencia a DocumentoComercial

        [ForeignKey(nameof(DocumentoId))]
        public virtual DocumentoComercial? Documento { get; set; }

        // --- Datos fiscales ---
        [Required, StringLength(20)]
        public string NifEmisor { get; set; } = string.Empty;

        [StringLength(150)]
        public string? NombreRazonEmisor { get; set; }

        [Required, StringLength(60)]
        public string NumeroFactura { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Serie { get; set; } = string.Empty;

        public DateTime FechaExpedicion { get; set; }

        [Required, StringLength(2)]
        public string TipoFactura { get; set; } = "F1"; // F1, F2, F3, R1-R5

        [StringLength(2)]
        public string? TipoRectificativa { get; set; } // I, S

        public bool Incidencia { get; set; } = false;

        public bool RechazoPrevio { get; set; } = false;

        [StringLength(60)]
        public string? RefExterna { get; set; }

        public string? FacturasRectificadasJson { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseImponible { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaRecargoEquivalencia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ImporteTotal { get; set; }

        [StringLength(3)]
        public string CodMoneda { get; set; } = "EUR";

        [StringLength(500)]
        public string? DescripcionOperacion { get; set; }

        public DateTimeOffset FechaHoraHusoGeneracion { get; set; }

        public bool EsPrimerRegistro { get; set; }

        [StringLength(64)]
        public string? HuellaAnterior { get; set; }

        [Required, StringLength(64)]
        public string Huella { get; set; } = string.Empty;

        [StringLength(2)]
        public string TipoHuella { get; set; } = "01"; // 01 SHA-256

        // Identificación sistema informático
        [StringLength(100)]
        public string? NombreSistemaInformatico { get; set; }

        [StringLength(20)]
        public string? VersionSistemaInformatico { get; set; }

        [StringLength(100)]
        public string? IdSistemaInformatico { get; set; }

        [StringLength(20)]
        public string? NumeroInstalacion { get; set; }

        // --- Específico TicketBAI ---
        [Required, StringLength(2)]
        public string HaciendaForal { get; set; } = string.Empty; // BI, GI, AV

        [Required, StringLength(100)]
        public string CertificadoSoftware { get; set; } = string.Empty; // Nombre certificado software

        [StringLength(200)]
        public string? CertificadoSoftwareHuella { get; set; } // Huella certificado

        [StringLength(100)]
        public string? IdCertificadoSoftware { get; set; }

        // Firma electrónica del registro
        [StringLength(500)]
        public string? FirmaRegistroBase64 { get; set; }

        // Envío a Hacienda Foral
        [Required, StringLength(20)]
        public string EstadoEnvio { get; set; } = "Pendiente"; // Pendiente, Enviado, Aceptado, Rechazado, Error

        public DateTimeOffset? FechaEnvio { get; set; }

        public int IntentosEnvio { get; set; } = 0;

        public string? RespuestaHacienda { get; set; }

        public string? CodigoErrorHacienda { get; set; }

        public string? UrlQrTicketBai { get; set; }

        [Required]
        public string DatosRegistroJson { get; set; } = string.Empty;

        // Trazabilidad anulación
        public DateTimeOffset? FechaAnulacion { get; set; }

        public int? AnulacionPreviaId { get; set; }

        // Auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>
    /// Configuración específica por Hacienda Foral (Bizkaia, Gipuzkoa, Álava).
    /// </summary>
    public class ConfiguracionTicketBai
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required, StringLength(2)]
        public string HaciendaForal { get; set; } = string.Empty; // BI, GI, AV

        [Required, StringLength(100)]
        public string CertificadoSoftwareNombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CertificadoSoftwareHuella { get; set; }

        [StringLength(100)]
        public string? IdCertificadoSoftware { get; set; }

        [Required, StringLength(100)]
        public string NombreSistemaInformatico { get; set; } = "ERP.NET";

        [StringLength(20)]
        public string? VersionSistemaInformatico { get; set; } = "1.0.0";

        [StringLength(100)]
        public string? IdSistemaInformatico { get; set; }

        [StringLength(20)]
        public string? NumeroInstalacion { get; set; }

        // Endpoints específicos por Hacienda Foral
        [StringLength(200)]
        public string? EndpointEnvio { get; set; }

        [StringLength(200)]
        public string? EndpointConsulta { get; set; }

        [StringLength(200)]
        public string? EndpointAnulacion { get; set; }

        // Configuración de envío
        public int IntentosMaximosEnvio { get; set; } = 3;

        public int IntervaloReintentoMinutos { get; set; } = 60;

        public bool EnvioAutomatico { get; set; } = true;

        // Auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaUltimaActualizacion { get; set; }

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        [StringLength(100)]
        public string? UsuarioUltimaActualizacion { get; set; }
    }

    /// <summary>
    /// Registro de anulación específico TicketBAI.
    /// </summary>
    public class TicketBaiAnulacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RegistroAltaId { get; set; }

        [ForeignKey(nameof(RegistroAltaId))]
        public virtual TicketBaiRegistro? RegistroAlta { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required, StringLength(20)]
        public string NifEmisor { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string NumeroFactura { get; set; } = string.Empty;

        public DateTime FechaExpedicion { get; set; }

        public DateTimeOffset FechaHoraHusoGeneracion { get; set; }

        [StringLength(64)]
        public string? HuellaAnterior { get; set; }

        [Required, StringLength(64)]
        public string Huella { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string EstadoEnvio { get; set; } = "Pendiente";

        public DateTimeOffset? FechaEnvio { get; set; }

        public int IntentosEnvio { get; set; } = 0;

        public string? RespuestaHacienda { get; set; }

        [Required]
        public string DatosRegistroJson { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset FechaAnulacion { get; set; } = DateTimeOffset.Now;

        [StringLength(500)]
        public string? MotivoAnulacion { get; set; }
    }
}