using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.Domain.Entities.Fiscal;

namespace ERP.Domain.Entities
{
    public class Empresa
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre comercial es obligatorio")]
        [StringLength(100)]
        public string NombreComercial { get; set; } = string.Empty;

        [Required(ErrorMessage = "La razón social es legalmente necesaria")]
        [StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [Required(ErrorMessage = "El CIF/NIF es imprescindible")]
        [StringLength(20)]
        public string CIF { get; set; } = string.Empty;

        public string? Direccion { get; set; }
        public string? CodigoPostal { get; set; }
        public string? Poblacion { get; set; }
        public string? Provincia { get; set; }
        
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Web { get; set; }

        // --- DATOS MERCANTILES ---
        public string? RegistroMercantil { get; set; }

        // --- ATRIBUTOS VISUALES ---
        public string? LogoUrl { get; set; } 
        public string? LogoBase64 { get; set; } 
        public string ColorHex { get; set; } = "#3498db"; 
        public string? Eslogan { get; set; }

        // --- CONFIGURACIÓN DE NEGOCIO ---
        [Required]
        public string SerieFacturacion { get; set; } = "2026";
        public int UltimoNumeroFactura { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal IvaDefecto { get; set; } = 21m;

        public bool IsActiva { get; set; } = true;

        // --- VERI*FACTU (RD 1007/2023 + Orden HAC/1177/2024) ---
        public ModalidadVerifactu ModalidadVerifactu { get; set; } = ModalidadVerifactu.Verifactu;
        public DateTime? FechaAltaVerifactu { get; set; }
        public int? CertificadoVerifactuId { get; set; }
        [ForeignKey(nameof(CertificadoVerifactuId))]
        public virtual FirmaDigital.CertificadoDigital? CertificadoVerifactu { get; set; }
        [StringLength(100)]
        public string? NombreSistemaInformatico { get; set; } = "ERP.NET";
        [StringLength(20)]
        public string? VersionSistemaInformatico { get; set; } = "1.0.0";
        [StringLength(100)]
        public string? IdSistemaInformatico { get; set; }
        [StringLength(20)]
        public string? NumeroInstalacion { get; set; }

        // --- TERRITORIO FISCAL (IVA/IGIC/IPSI) ---
        public TerritorioFiscal TerritorioFiscal { get; set; } = TerritorioFiscal.PeninsulaBaleares;
        public bool EsSII { get; set; } = false; // Suministro Inmediato Información

        // --- AUDITORÍA ---
        public DateTime FechaAlta { get; set; } = DateTime.Now;
        public DateTime? UltimaModificacion { get; set; }
    }
}