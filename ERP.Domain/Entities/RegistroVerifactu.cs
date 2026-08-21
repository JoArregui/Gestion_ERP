using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities;

/// <summary>Registro de facturación de alta conservado para la remisión VERI*FACTU.</summary>
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

    [Required, StringLength(60)]
    public string NumeroFactura { get; set; } = string.Empty;

    public DateTime FechaExpedicion { get; set; }

    [Required, StringLength(2)]
    public string TipoFactura { get; set; } = "F1";

    [Column(TypeName = "decimal(18,2)")]
    public decimal CuotaTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ImporteTotal { get; set; }

    public DateTimeOffset FechaHoraHusoGeneracion { get; set; }

    public bool EsPrimerRegistro { get; set; }

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

    [Required, StringLength(500)]
    public string UrlQr { get; set; } = string.Empty;
}
