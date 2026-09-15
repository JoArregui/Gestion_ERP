using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.DTOs.Bancario
{
    // ── Cuenta Bancaria ────────────────────────────────────────────────
    public class CrearCuentaBancariaDto
    {
        [Required, StringLength(34)] public string IBAN { get; set; } = string.Empty;
        [StringLength(11)] public string? BIC { get; set; }
        [Required, StringLength(100)] public string NombreCuenta { get; set; } = string.Empty;
        [StringLength(100)] public string? EntidadBancaria { get; set; }
        [StringLength(35)] public string? CreditorIdentifier { get; set; }
        public string Tipo { get; set; } = "Operativa"; // TipoCuentaBancaria enum name
        public bool EsPrincipal { get; set; }
        public bool PermiteTransferenciasSEPA { get; set; } = true;
        public bool PermiteAdeudosSEPA { get; set; }
        public bool PermiteTransferenciasInstant { get; set; }
        [Range(0, 999999999.99)] public decimal? LimiteDiarioTransferencias { get; set; }
        [Range(0, 999999999.99)] public decimal? LimiteDiarioAdeudos { get; set; }
    }

    public class CuentaBancariaDto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string IBAN { get; set; } = string.Empty;
        public string IBANFormateado { get; set; } = string.Empty;
        public string? BIC { get; set; }
        public string NombreCuenta { get; set; } = string.Empty;
        public string? EntidadBancaria { get; set; }
        public string? CreditorIdentifier { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public bool EsPrincipal { get; set; }
        public bool Activa { get; set; }
        public bool PermiteTransferenciasSEPA { get; set; }
        public bool PermiteAdeudosSEPA { get; set; }
        public bool PermiteTransferenciasInstant { get; set; }
        public decimal? LimiteDiarioTransferencias { get; set; }
        public decimal? LimiteDiarioAdeudos { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    // ── Mandato SEPA ──────────────────────────────────────────────────
    public class CrearMandatoSepaDto
    {
        [Required, StringLength(35)] public string? ReferenciaUnicaMandato { get; set; } // auto si vacío
        [Required, StringLength(35)] public string CreditorIdentifier { get; set; } = string.Empty;
        [Required, StringLength(140)] public string DeudorNombre { get; set; } = string.Empty;
        [StringLength(140)] public string? DeudorNombreComercial { get; set; }
        [StringLength(70)] public string? DeudorCalle { get; set; }
        [StringLength(16)] public string? DeudorNumero { get; set; }
        [StringLength(16)] public string? DeudorCodigoPostal { get; set; }
        [StringLength(35)] public string? DeudorPoblacion { get; set; }
        [StringLength(2)] public string? DeudorPais { get; set; } = "ES";
        [Required, StringLength(34)] public string DeudorIBAN { get; set; } = string.Empty;
        [StringLength(11)] public string? DeudorBIC { get; set; }
        [StringLength(20)] public string? DeudorIdentificacionFiscal { get; set; }
        [Required, StringLength(140)] public string AcreedorNombre { get; set; } = string.Empty;
        [Required, StringLength(34)] public string AcreedorIBAN { get; set; } = string.Empty;
        public string Esquema { get; set; } = "Core"; // Core | B2B
        public string TipoSecuencia { get; set; } = "RCUR"; // FRST|RCUR|FNAL|OOFF
        public DateTime FechaFirma { get; set; } = DateTime.Now;
        public int? ClienteId { get; set; }
        public int? CuentaBancariaAcreedorId { get; set; }
        public int? CuentaBancariaDeudorId { get; set; }
    }

    public class MandatoSepaDto
    {
        public int Id { get; set; }
        public string ReferenciaUnicaMandato { get; set; } = string.Empty;
        public string RUMFormateado { get; set; } = string.Empty;
        public string CreditorIdentifier { get; set; } = string.Empty;
        public string DeudorNombre { get; set; } = string.Empty;
        public string DeudorIBAN { get; set; } = string.Empty;
        public string AcreedorNombre { get; set; } = string.Empty;
        public string AcreedorIBAN { get; set; } = string.Empty;
        public string Esquema { get; set; } = string.Empty;
        public string TipoSecuencia { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public bool BeneficiarioVerificado { get; set; }
        public DateTime FechaFirma { get; set; }
        public bool EstaVigente { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    // ── Remesa SEPA ───────────────────────────────────────────────────
    public class CrearRemesaTransferenciaDto
    {
        [Required] public int CuentaOrdenanteId { get; set; }
        public DateTime FechaEjecucion { get; set; } = DateTime.Now.AddDays(1);
        [Required, MinLength(1)] public List<CrearOperacionRemesaDto> Operaciones { get; set; } = new();
    }

    public class CrearRemesaAdeudoDto : CrearRemesaTransferenciaDto
    {
        [Required] public int CuentaAcreedoraId { get; set; }
        public string Esquema { get; set; } = "SDD_Core"; // SDD_Core | SDD_B2B
    }

    public class CrearOperacionRemesaDto
    {
        // Transferencia: Beneficiario*  | Adeudo: Deudor* + Mandato
        [StringLength(140)] public string? BeneficiarioNombre { get; set; }
        [StringLength(34)] public string? BeneficiarioIBAN { get; set; }
        [StringLength(11)] public string? BeneficiarioBIC { get; set; }
        [StringLength(70)] public string? BeneficiarioCalle { get; set; }
        [StringLength(16)] public string? BeneficiarioNumero { get; set; }
        [StringLength(16)] public string? BeneficiarioCodigoPostal { get; set; }
        [StringLength(35)] public string? BeneficiarioPoblacion { get; set; }
        [StringLength(2)] public string? BeneficiarioPais { get; set; } = "ES";

        [StringLength(140)] public string? DeudorNombre { get; set; }
        [StringLength(34)] public string? DeudorIBAN { get; set; }
        public int? MandatoId { get; set; }
        [StringLength(35)] public string? ReferenciaUnicaMandato { get; set; }
        public string TipoSecuencia { get; set; } = "RCUR";

        [Required, Range(0.01, 999999999.99)] public decimal Importe { get; set; }
        [StringLength(3)] public string Moneda { get; set; } = "EUR";
        [StringLength(140)] public string? Concepto { get; set; }
        [StringLength(35)] public string? ReferenciaPropia { get; set; }
        public string? OrigenTipo { get; set; }
        public int? OrigenId { get; set; }
    }

    public class RemesaSepaDto
    {
        public int Id { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Esquema { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaEjecucion { get; set; }
        public decimal ImporteTotal { get; set; }
        public int NumeroOperaciones { get; set; }
        public string? NombreArchivoXml { get; set; }
        public string? HashXmlSHA256 { get; set; }
        public string TipoFicheroISO { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public List<OperacionRemesaDto> Operaciones { get; set; } = new();
    }

    public class OperacionRemesaDto
    {
        public int Id { get; set; }
        public int Orden { get; set; }
        public string? BeneficiarioNombre { get; set; }
        public string? BeneficiarioIBAN { get; set; }
        public string? DeudorNombre { get; set; }
        public string? DeudorIBAN { get; set; }
        public decimal Importe { get; set; }
        public string Moneda { get; set; } = "EUR";
        public string? Concepto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? CodigoRespuestaBanco { get; set; }
    }

    // ── Extracto / Conciliación ───────────────────────────────────────
    public class ImportarExtractoDto
    {
        [Required] public int CuentaBancariaId { get; set; }
        [Required] public string XmlCamt053 { get; set; } = string.Empty;
    }

    public class ExtractoBancarioDto
    {
        public int Id { get; set; }
        public string ReferenciaExtracto { get; set; } = string.Empty;
        public DateTime FechaExtracto { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal SaldoFinal { get; set; }
        public decimal TotalCargos { get; set; }
        public decimal TotalAbonos { get; set; }
        public bool Procesado { get; set; }
        public int MovimientosCount { get; set; }
    }

    public class MovimientoExtractoDto
    {
        public int Id { get; set; }
        public DateTime FechaValor { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public string? Concepto { get; set; }
        public string? ContrapartidaNombre { get; set; }
        public string? ContrapartidaIBAN { get; set; }
        public bool Conciliado { get; set; }
        public string? EndToEndId { get; set; }
    }
}
