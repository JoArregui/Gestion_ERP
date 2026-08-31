using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities;

/// <summary>Registro de facturación de alta conservado para la remisión VERI*FACTU (RD 1007/2023 + Orden HAC/1177/2024).</summary>
public class RegistroVerifactu
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentoId { get; set; }

    [ForeignKey(nameof(DocumentoId))]
    public DocumentoComercial? Documento { get; set; }

    [Required]
    public int EmpresaId { get; set; }

    [Required, StringLength(20)]
    public string NifEmisor { get; set; } = string.Empty;

    // Nombre/Razón emisor (Orden HAC/1177/2024 — aparece en QR/verificación)
    [StringLength(120)]
    public string? NombreRazonEmisor { get; set; }

    [Required, StringLength(60)]
    public string NumeroFactura { get; set; } = string.Empty;

    public DateTime FechaExpedicion { get; set; }

    [Required, StringLength(2)]
    public string TipoFactura { get; set; } = "F1"; // F1,F2,F3,R1-R5

    // Rectificativas
    [StringLength(2)]
    public string? TipoRectificativa { get; set; } // I,S

    // Incidencia y rechazo previo (art. 10.2 Orden)
    public bool Incidencia { get; set; } = false;
    public bool RechazoPrevio { get; set; } = false;

    [StringLength(60)]
    public string? RefExterna { get; set; }

    // Facturas rectificadas/sustituidas serializadas (JSON)
    public string? FacturasRectificadasJson { get; set; }
    public string? FacturasSustituidasJson { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseImponible { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CuotaTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CuotaRecargoEquivalencia { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ImporteTotal { get; set; }

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

    // Identificación sistema informático (declaración responsable)
    [StringLength(100)]
    public string? NombreSistemaInformatico { get; set; }
    [StringLength(20)]
    public string? VersionSistemaInformatico { get; set; }
    [StringLength(100)]
    public string? IdSistemaInformatico { get; set; }
    [StringLength(20)]
    public string? NumeroInstalacion { get; set; }

    [Required, StringLength(20)]
    public string EstadoRemision { get; set; } = "Pendiente"; // Pendiente, Enviado, Aceptado, Rechazado

    public DateTimeOffset? FechaRemision { get; set; }
    public int IntentosRemision { get; set; }
    public string? RespuestaAeat { get; set; }
    public string? CodigoErrorAeat { get; set; }

    [Required]
    public string DatosRegistroJson { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string UrlQr { get; set; } = string.Empty;

    // Firma electrónica del registro (opcional según modalidad)
    public string? FirmaRegistroBase64 { get; set; }
}

/// <summary>Registro de anulación VERI*FACTU (mismo encadenamiento).</summary>
public class RegistroVerifactuAnulacion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RegistroAltaId { get; set; }

    [ForeignKey(nameof(RegistroAltaId))]
    public RegistroVerifactu? RegistroAlta { get; set; }

    [Required]
    public int EmpresaId { get; set; }

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
    public string EstadoRemision { get; set; } = "Pendiente";

    public DateTimeOffset? FechaRemision { get; set; }
    public int IntentosRemision { get; set; }
    public string? RespuestaAeat { get; set; }

    [Required]
    public string DatosRegistroJson { get; set; } = string.Empty;
}

public enum ModalidadVerifactu
{
    NoVerifactu = 0,
    Verifactu = 1
}
