using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Bancario
{
    /// <summary>
    /// Mandato SEPA (SDD Core / SDD B2B) - AutorizaciÃ³n de deudor a acreedor para adeudos directos
    /// Regulado por Reglamento (UE) 260/2012 + EPC Rulebooks
    /// VerificaciÃ³n de beneficiario obligatoria desde oct 2025 (PSD2)
    /// Direcciones estructuradas ISO 20022 obligatorias nov 2026
    /// </summary>
    public class MandatoSEPA
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; } // Empresa acreedora (nuestra empresa)

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        // Referencia Ãºnica de mandato (RUM) - obligatoria Ãºnica por mandato
        [Required]
        [StringLength(35)]
        public string ReferenciaUnicaMandato { get; set; } = string.Empty; // RUM

        // Identificador del acreedor (Creditor Identifier) - asignado por AEAT/Banco de EspaÃ±a
        [Required]
        [StringLength(35)]
        public string CreditorIdentifier { get; set; } = string.Empty;

        // Datos del DEUDOR (cliente que paga)
        [Required]
        [StringLength(140)]
        public string DeudorNombre { get; set; } = string.Empty;

        [StringLength(140)]
        public string? DeudorNombreComercial { get; set; }

        // DirecciÃ³n estructurada ISO 20022 (obligatoria nov 2026)
        [StringLength(70)]
        public string? DeudorCalle { get; set; }

        [StringLength(16)]
        public string? DeudorNumero { get; set; }

        [StringLength(16)]
        public string? DeudorCodigoPostal { get; set; }

        [StringLength(35)]
        public string? DeudorPoblacion { get; set; }

        [StringLength(2)]
        public string? DeudorPais { get; set; } = "ES";

        [Required]
        [StringLength(34)]
        public string DeudorIBAN { get; set; } = string.Empty;

        [StringLength(11)]
        public string? DeudorBIC { get; set; }

        // IdentificaciÃ³n fiscal deudor
        [StringLength(20)]
        public string? DeudorIdentificacionFiscal { get; set; } // NIF/CIF

        // Datos del ACREEDOR (nuestra empresa)
        [Required]
        [StringLength(140)]
        public string AcreedorNombre { get; set; } = string.Empty;

        [StringLength(140)]
        public string? AcreedorNombreComercial { get; set; }

        // DirecciÃ³n estructurada acreedor
        [StringLength(70)]
        public string? AcreedorCalle { get; set; }

        [StringLength(16)]
        public string? AcreedorNumero { get; set; }

        [StringLength(16)]
        public string? AcreedorCodigoPostal { get; set; }

        [StringLength(35)]
        public string? AcreedorPoblacion { get; set; }

        [StringLength(2)]
        public string? AcreedorPais { get; set; } = "ES";

        [Required]
        [StringLength(34)]
        public string AcreedorIBAN { get; set; } = string.Empty;

        [StringLength(11)]
        public string? AcreedorBIC { get; set; }

        [Required]
        [StringLength(35)]
        public string AcreedorCreditorIdentifier { get; set; } = string.Empty;

        // Tipo de esquema SEPA
        public EsquemaSEPA Esquema { get; set; } = EsquemaSEPA.Core; // Core (consumidores) / B2B (empresas)

        // Tipo de secuencia
        public TipoSecuenciaMandato TipoSecuencia { get; set; } = TipoSecuenciaMandato.RCUR; // FRST, RCUR, FNAL, OOFF

        // Fecha firma mandato
        [Required]
        public DateTime FechaFirma { get; set; } = DateTime.Now;

        // Fecha primera presentaciÃ³n (FRST) / Ãºltima (FNAL)
        public DateTime? FechaPrimeraPresentacion { get; set; }
        public DateTime? FechaUltimaPresentacion { get; set; }

        // Estado del mandato
        public EstadoMandatoSEPA Estado { get; set; } = EstadoMandatoSEPA.PendienteFirma;

        // VerificaciÃ³n beneficiario (PSD2 - obligatoria desde oct 2025)
        public bool BeneficiarioVerificado { get; set; } = false;
        public DateTime? FechaVerificacionBeneficiario { get; set; }
        public string? MetodoVerificacion { get; set; } // "IBAN_Name_Check", "Microdeposito", "OpenBanking"

        // InformaciÃ³n adicional (mÃ¡x 140 chars en SDD)
        [StringLength(140)]
        public string? InformacionAdicional { get; set; } // Referencia factura, concepto, etc.

        // Referencia a cliente/proveedor en ERP
        public int? ClienteId { get; set; } // Si deudor es cliente
        public int? ProveedorId { get; set; } // Si acreedor es proveedor (para mandatos recibidos)

        // Cuenta bancaria asociada
        public int? CuentaBancariaAcreedorId { get; set; }
        [ForeignKey(nameof(CuentaBancariaAcreedorId))]
        public virtual CuentaBancaria? CuentaBancariaAcreedor { get; set; }

        public int? CuentaBancariaDeudorId { get; set; }
        [ForeignKey(nameof(CuentaBancariaDeudorId))]
        public virtual CuentaBancaria? CuentaBancariaDeudor { get; set; }

        // AuditorÃ­a
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }

        // NavegaciÃ³n
        public virtual ICollection<RemesaSEPA> Remesas { get; set; } = new List<RemesaSEPA>();

        // Propiedades calculadas
        [NotMapped]
        public string RUMFormateado => FormatearRUM(ReferenciaUnicaMandato);

        [NotMapped]
        public bool EstaVigente => Estado == EstadoMandatoSEPA.Activo && 
                                   (FechaUltimaPresentacion == null || FechaUltimaPresentacion > DateTime.Now.AddMonths(-13)); // Caduca a 13 meses sin uso

        private static string FormatearRUM(string rum)
        {
            if (string.IsNullOrEmpty(rum)) return rum;
            return string.Join(" ", System.Text.RegularExpressions.Regex.Split(rum.Trim(), @"(.{4})").Where(s => !string.IsNullOrEmpty(s)));
        }
    }

    public enum EsquemaSEPA
    {
        Core = 0,     // SDD Core - consumidores y empresas (derecho devoluciÃ³n 8 semanas)
        B2B = 1       // SDD B2B - solo empresas (sin derecho devoluciÃ³n sin causa)
    }

    public enum TipoSecuenciaMandato
    {
        FRST = 0,     // Primera presentaciÃ³n
        RCUR = 1,     // Recurrente (normal)
        FNAL = 2,     // Ãšltima presentaciÃ³n
        OOFF = 3      // Ãšnica (one-off)
    }

    public enum EstadoMandatoSEPA
    {
        PendienteFirma = 0,
        Activo = 1,
        Suspendido = 2,
        Revocado = 3,
        Expirado = 4,      // 13 meses sin uso
        Rechazado = 5      // Banco rechazÃ³
    }
}