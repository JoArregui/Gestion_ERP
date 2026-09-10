using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Bancario
{
    /// <summary>
    /// Cuenta bancaria de la empresa (ordenante o beneficiaria)
    /// </summary>
    public class CuentaBancaria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(34)]
        public string IBAN { get; set; } = string.Empty;

        [StringLength(11)]
        public string? BIC { get; set; } // SWIFT/BIC (opcional desde 2016)

        [Required]
        [StringLength(100)]
        public string NombreCuenta { get; set; } = string.Empty; // "Cuenta Operativa", "Cuenta Nóminas", etc.

        [StringLength(100)]
        public string? EntidadBancaria { get; set; }

        [StringLength(4)]
        public string? CodigoEntidad { get; set; } // Código banco (ej: 0049 Santander)

        [StringLength(4)]
        public string? CodigoOficina { get; set; }

        [StringLength(2)]
        public string? DigitosControl { get; set; }

        [StringLength(10)]
        public string? NumeroCuenta { get; set; }

        public TipoCuentaBancaria Tipo { get; set; } = TipoCuentaBancaria.Operativa;

        public bool EsPrincipal { get; set; } = false;

        public bool Activa { get; set; } = true;

        // Para SEPA: Creditor Identifier (para emitir adeudos)
        [StringLength(35)]
        public string? CreditorIdentifier { get; set; } // Formato: ESXXZZZ... (AEAT asigna)

        // Configuración SEPA
        public bool PermiteTransferenciasSEPA { get; set; } = true;
        public bool PermiteAdeudosSEPA { get; set; } = false;
        public bool PermiteTransferenciasInstant { get; set; } = false;

        // Límites operativos
        [Column(TypeName = "decimal(18,2)")]
        public decimal? LimiteDiarioTransferencias { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LimiteDiarioAdeudos { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        // Navegación
        public virtual ICollection<MandatoSEPA> MandatosAcreedor { get; set; } = new List<MandatoSEPA>();
        public virtual ICollection<MandatoSEPA> MandatosDeudor { get; set; } = new List<MandatoSEPA>();
        public virtual ICollection<RemesaSEPA> RemesasOrdenante { get; set; } = new List<RemesaSEPA>();
        public virtual ICollection<RemesaSEPA> RemesasBeneficiaria { get; set; } = new List<RemesaSEPA>();

        // Propiedad calculada
        public string IBANFormateado => FormatearIBAN(IBAN);

        private static string FormatearIBAN(string iban)
        {
            if (string.IsNullOrEmpty(iban)) return iban;
            return string.Join(" ", System.Text.RegularExpressions.Regex.Split(iban.Trim(), @"(.{4})").Where(s => !string.IsNullOrEmpty(s)));
        }
    }

    public enum TipoCuentaBancaria
    {
        Operativa = 0,
        Nominas = 1,
        Proveedores = 2,
        Tesoreria = 3,
        Ahorro = 4,
        Credito = 5
    }
}