using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    /// <summary>
    /// Factura electrónica FACe (B2G Ley 25/2013) y B2B (Ley 18/2022 Crea y Crece)
    /// Formato Facturae 3.2.2 o EN16931 UBL/CEFACT, firma XAdES-Enveloped
    /// </summary>
    public class FacturaElectronica
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentoId { get; set; }

        [ForeignKey(nameof(DocumentoId))]
        public virtual DocumentoComercial? Documento { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public FormatoFacturaElectronica Formato { get; set; } = FormatoFacturaElectronica.Facturae32;

        [Required, StringLength(10)]
        public string Version { get; set; } = "3.2.2";

        // --- Nuevos campos cumplimiento legal facturación (añadidos 2026) ---
        [StringLength(20)]
        public string Serie { get; set; } = string.Empty; // Serie factura (obligatorio B2B/B2G)

        [StringLength(50)]
        public string NumeroExpedicion { get; set; } = string.Empty; // Número expedición oficial

        public byte? TipoOperacion { get; set; } // 0=Venta, 1=Devolución, 2=Intracomunitaria, etc.

        [StringLength(3)]
        public string CodMoneda { get; set; } = "EUR"; // Moneda obligatoria (por defecto EUR)

        [StringLength(20)]
        public string? NIFCliente { get; set; } // NIF cliente B2B

        [StringLength(120)]
        public string? NombreCliente { get; set; } // Nombre/Razón social cliente B2B

        // XML generado (Base64 para no romper collation) y hash
        public string? XmlBase64 { get; set; }
        [StringLength(64)]
        public string? HashSha256 { get; set; }

        // Firma XAdES
        public string? FirmaXAdESBase64 { get; set; }
        public int? CertificadoId { get; set; }
        [ForeignKey(nameof(CertificadoId))]
        public virtual FirmaDigital.CertificadoDigital? Certificado { get; set; }

        // DIR3 FACe (Oficina Contable / Órgano Gestor / Unidad Tramitadora)
        [StringLength(20)]
        public string? DIR3_OficinaContable { get; set; }
        [StringLength(20)]
        public string? DIR3_OrganoGestor { get; set; }
        [StringLength(20)]
        public string? DIR3_UnidadTramitadora { get; set; }

        // Punto de entrada
        public PuntoEntradaFacturaElectronica PuntoEntrada { get; set; } = PuntoEntradaFacturaElectronica.FACe;

        // Estados FACe / B2B
        public EstadoFacturaElectronica Estado { get; set; } = EstadoFacturaElectronica.Borrador;

        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaAcuseRecibo { get; set; }
        public string? CodigoRegistroFACe { get; set; } // REG...
        public string? MensajeEstado { get; set; }

        // B2B: plataforma privada + hub AEAT
        [StringLength(100)]
        public string? PlataformaB2B { get; set; }
        public string? AcuseReciboB2BJson { get; set; }

        // Plazo pago (Ley 3/2004 morosidad: 30 días AA.PP., 60 días B2B)
        public DateTime? FechaLimitePago { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    public enum FormatoFacturaElectronica
    {
        Facturae32 = 0,   // Facturae 3.2 / 3.2.2 (AEAT)
        UBL21 = 1,        // EN16931 UBL 2.1 (PEPPOL BIS Billing 3.0)
        CEFACT = 2,       // CII
        PDF_A3 = 3        // Factura híbrida PDF/A-3 con XML embebido
    }

    public enum PuntoEntradaFacturaElectronica
    {
        FACe = 0,         // FACe (Estado)
        FACeB2B = 1,      // Punto autonómico (eFACT Catalunya etc.)
        PrivadoB2B = 2,   // Plataforma privada certificada
        HubAEAT = 3,      // Solución pública AEAT (Crea y Crece)
        Email = 4         // Transitorio
    }

    public enum EstadoFacturaElectronica
    {
        Borrador = 0,
        Generada = 1,
        Firmada = 2,
        Registrada = 3,       // En FACe / plataforma
        EnTramite = 4,
        Aceptada = 5,
        Rechazada = 6,
        Pagada = 7,
        Anulada = 8,
        RechazadaTecnica = 9  // XSD / firma inválida
    }
}