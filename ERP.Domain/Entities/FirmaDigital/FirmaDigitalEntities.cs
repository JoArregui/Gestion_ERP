using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.FirmaDigital
{
    public class CertificadoDigital
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa Empresa { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public TipoCertificado Tipo { get; set; } = TipoCertificado.Avanzada;

        [Required]
        public EstadoCertificado Estado { get; set; } = EstadoCertificado.Vigente;

        [Required]
        [StringLength(200)]
        public string SubjectDN { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string IssuerDN { get; set; } = string.Empty;

        [Required]
        public DateTime NotBefore { get; set; }

        [Required]
        public DateTime NotAfter { get; set; }

        [Required]
        [StringLength(40)]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string ThumbprintSHA256 { get; set; } = string.Empty;

        [Required]
        [StringLength(40)]
        public string ThumbprintSHA1 { get; set; } = string.Empty;

        [Required]
        public string PublicKeyPem { get; set; } = string.Empty;

        [StringLength(50)]
        public string AlgoritmoFirma { get; set; } = "SHA256withRSA";

        [StringLength(50)]
        public string AlgoritmoClave { get; set; } = "RSA";

        public int TamanoClave { get; set; } = 2048;

        public UsoCertificado Uso { get; set; } = UsoCertificado.Facturacion;

        public AlmacenamientoCertificado Almacenamiento { get; set; } = AlmacenamientoCertificado.HSM;

        public bool EsCualificado => Tipo == TipoCertificado.Cualificada;

        [StringLength(100)]
        public string? QTSP { get; set; }

        [StringLength(100)]
        public string? NumeroAutorizacionQTSP { get; set; }

        [StringLength(100)]
        public string? PoliticaFirmaOID { get; set; }

        public string? CadenaCertificadosPem { get; set; }

        [StringLength(200)]
        public string? OCSPUrl { get; set; }
        [StringLength(200)]
        public string? CRLUrl { get; set; }

        public bool Revocado { get; set; } = false;
        public DateTime? FechaRevoca { get; set; }
        public string? MotivoRevoca { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }

        public virtual ICollection<FirmaElectronica> FirmasRealizadas { get; set; } = new List<FirmaElectronica>();

        [NotMapped]
        public bool EstaVigente => Estado == EstadoCertificado.Vigente && !Revocado && DateTime.Now >= NotBefore && DateTime.Now <= NotAfter;

        [NotMapped]
        public int DiasParaExpiracion => (int)(NotAfter - DateTime.Now).TotalDays;

        [NotMapped]
        public bool EstaProximoExpirar => DiasParaExpiracion <= 30 && DiasParaExpiracion > 0;
    }

    public class FirmaElectronica
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa Empresa { get; set; } = null!;

        [Required]
        public int CertificadoId { get; set; }
        [ForeignKey(nameof(CertificadoId))]
        public virtual CertificadoDigital Certificado { get; set; } = null!;

        [Required]
        public TipoFirma Tipo { get; set; } = TipoFirma.Avanzada;

        [Required]
        public FormatoFirma Formato { get; set; } = FormatoFirma.PAdES;

        [Required]
        [StringLength(100)]
        public string DocumentoTipo { get; set; } = string.Empty;

        [Required]
        public int DocumentoId { get; set; }

        [StringLength(100)]
        public string? DocumentoReferencia { get; set; }

        [Required]
        [StringLength(64)]
        public string HashDocumentoSHA256 { get; set; } = string.Empty;

        [StringLength(40)]
        public string? HashDocumentoSHA1 { get; set; }

        [Required]
        public string FirmaBase64 { get; set; } = string.Empty;

        [Required]
        public string FirmaEstructurada { get; set; } = string.Empty;

        public DateTime FechaFirma { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? TSPUrl { get; set; }

        public string? TimestampTokenBase64 { get; set; }

        public string? PoliticaFirmaOID { get; set; }

        [Required]
        [StringLength(200)]
        public string FirmanteNombre { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FirmanteNIF { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FirmanteCargo { get; set; }

        [StringLength(100)]
        public string? FirmanteEmail { get; set; }

        [StringLength(45)]
        public string? DireccionIP { get; set; }

        [StringLength(200)]
        public string? UserAgent { get; set; }

        [StringLength(100)]
        public string? Geolocalizacion { get; set; }

        public EstadoVerificacionFirma EstadoVerificacion { get; set; } = EstadoVerificacionFirma.Valida;

        public DateTime? FechaUltimaVerificacion { get; set; }

        [StringLength(500)]
        public string? DetalleVerificacion { get; set; }

        public string? ArchivoFirmadoBase64 { get; set; }

        [StringLength(100)]
        public string? NombreArchivoFirmado { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        [NotMapped]
        public bool EsValidaLegal => Tipo == TipoFirma.Cualificada || (Tipo == TipoFirma.Avanzada && EstadoVerificacion == EstadoVerificacionFirma.Valida);

        [NotMapped]
        public string TipoFirmaLegal
        {
            get
            {
                return Tipo switch
                {
                    TipoFirma.Cualificada => "Firma Electrónica Cualificada (equivalente firma manuscrita - Art. 25 eIDAS)",
                    TipoFirma.Avanzada => "Firma Electrónica Avanzada (Art. 26 eIDAS)",
                    _ => "Firma Electrónica Simple"
                };
            }
        }
    }

    public class SolicitudFirma
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa Empresa { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Referencia { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        public string DocumentoBase64 { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string HashDocumentoSHA256 { get; set; } = string.Empty;

        public string FirmantesJson { get; set; } = "[]";

        public TipoFirma TipoFirmaRequerida { get; set; } = TipoFirma.Avanzada;
        public FormatoFirma FormatoSalida { get; set; } = FormatoFirma.PAdES;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaExpiracion { get; set; } = DateTime.Now.AddDays(30);

        public EstadoSolicitudFirma Estado { get; set; } = EstadoSolicitudFirma.Pendiente;

        public int RecordatoriosEnviados { get; set; } = 0;
        public DateTime? UltimoRecordatorio { get; set; }

        public int? FirmaElectronicaId { get; set; }
        [ForeignKey(nameof(FirmaElectronicaId))]
        public virtual FirmaElectronica FirmaResultado { get; set; } = null!;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    public class SelloTiempo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa Empresa { get; set; } = null!;

        [Required]
        [StringLength(64)]
        public string HashDatosSHA256 { get; set; } = string.Empty;

        [Required]
        public string TokenBase64 { get; set; } = string.Empty;

        [Required]
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        [Required]
        public DateTime FechaTimestamp { get; set; }

        [Required]
        [StringLength(200)]
        public string TSAUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TSAName { get; set; } = string.Empty;

        [StringLength(64)]
        public string? TSACertThumbprint { get; set; }

        public string? PoliticaTSA_OID { get; set; }

        [StringLength(100)]
        public string? ReferenciaDocumento { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    public class ComunicacionCertificada
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa Empresa { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Referencia { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Asunto { get; set; } = string.Empty;

        [Required]
        public string Contenido { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string RemitenteNombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RemitenteEmail { get; set; } = string.Empty;

        public string DestinatariosJson { get; set; } = "[]";

        public string AdjuntosJson { get; set; } = "[]";

        public bool RequiereAcuseRecibo { get; set; } = true;
        public bool RequiereEntregaPersonal { get; set; } = false;
        public DateTime? FechaLimiteEntrega { get; set; }

        public EstadoComunicacionCertificada Estado { get; set; } = EstadoComunicacionCertificada.PendienteEnvio;

        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaAcuseRecibo { get; set; }

        public string? PruebaEntregaBase64 { get; set; }
        public string? PruebaContenidoBase64 { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    public enum TipoCertificado
    {
        Simple = 0,
        Avanzada = 1,
        Cualificada = 2
    }

    public enum EstadoCertificado
    {
        Pendiente = 0,
        Vigente = 1,
        Expirado = 2,
        Revocado = 3,
        Suspendido = 4
    }

    public enum UsoCertificado
    {
        Facturacion = 0,
        Contratos = 1,
        Nominas = 2,
        Bancario = 3,
        MandatosSEPA = 4,
        ComunicacionesCertificadas = 5,
        General = 6
    }

    public enum AlmacenamientoCertificado
    {
        HSM = 0,
        Software = 1,
        TokenUSB = 2,
        Cloud = 3
    }

    public enum TipoFirma
    {
        Simple = 0,
        Avanzada = 1,
        Cualificada = 2
    }

    public enum FormatoFirma
    {
        PAdES = 0,
        XAdES = 1,
        CAdES = 2,
        JAdES = 3
    }

    public enum EstadoVerificacionFirma
    {
        Valida = 0,
        InvalidaCertificado = 1,
        InvalidaCadena = 2,
        InvalidaTimestamp = 3,
        InvalidaHash = 4,
        InvalidaPolitica = 5,
        CertificadoExpirado = 6,
        CertificadoRevocado = 7,
        ErrorTecnico = 8
    }

    public enum EstadoSolicitudFirma
    {
        Pendiente = 0,
        Enviada = 1,
        Firmada = 2,
        Rechazada = 3,
        Expirada = 4,
        Cancelada = 5
    }

    public enum EstadoComunicacionCertificada
    {
        PendienteEnvio = 0,
        Enviada = 1,
        Entregada = 2,
        Leida = 3,
        AcuseRecibido = 4,
        Fallida = 5,
        Expirada = 6
    }
}