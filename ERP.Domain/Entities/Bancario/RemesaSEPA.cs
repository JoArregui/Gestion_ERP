using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Bancario
{
    /// <summary>
    /// Remesa SEPA - AgrupaciÃ³n de operaciones para envÃ­o al banco (pain.001 / pain.008)
    /// Formato ISO 20022 XML: pain.001.001.03 (SCT) / pain.008.001.02 (SDD)
    /// </summary>
    public class RemesaSEPA
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(20)]
        public string Referencia { get; set; } = string.Empty; // Ej: "REM-2026-000001"

        [Required]
        public TipoRemesaSEPA Tipo { get; set; } // Transferencia / Adeudo

        [Required]
        public EsquemaRemesaSEPA Esquema { get; set; } // SCT / SDD Core / SDD B2B / SCT Inst

        [Required]
        public int CuentaBancariaOrdenanteId { get; set; }
        [ForeignKey(nameof(CuentaBancariaOrdenanteId))]
        public virtual CuentaBancaria? CuentaBancariaOrdenante { get; set; }

        // Para adeudos: cuenta acreedora (nuestra)
        public int? CuentaBancariaAcreedoraId { get; set; }
        [ForeignKey(nameof(CuentaBancariaAcreedoraId))]
        public virtual CuentaBancaria? CuentaBancariaAcreedora { get; set; }

        // Fecha valor / ejecuciÃ³n deseada
        [Required]
        public DateTime FechaEjecucion { get; set; } = DateTime.Now.AddDays(1); // D+1 estÃ¡ndar

        // Estado de la remesa
        public EstadoRemesaSEPA Estado { get; set; } = EstadoRemesaSEPA.Borrador;

        // Totales
        [Column(TypeName = "decimal(18,2)")]
        public decimal ImporteTotal { get; set; }

        public int NumeroOperaciones { get; set; }

        // Fichero ISO 20022 generado
        public string? XmlGenerado { get; set; } // Contenido XML pain.001/pain.008
        public string? NombreArchivoXml { get; set; } // Ej: "pain.001_20260828_000001.xml"
        public string? HashXmlSHA256 { get; set; }
        public long? TamanoBytes { get; set; }

        // EnvÃ­o al banco
        public DateTime? FechaEnvioBanco { get; set; }
        public string? ReferenciaBanco { get; set; } // Referencia asignada por banco
        public string? RespuestaBanco { get; set; } // XML respuesta (pain.002 / camt.054)

        // ConciliaciÃ³n
        public bool Conciliada { get; set; } = false;
        public DateTime? FechaConciliacion { get; set; }

        // AuditorÃ­a
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }
        [StringLength(100)]
        public string? UsuarioEnvio { get; set; }
        [StringLength(100)]
        public string? UsuarioConciliacion { get; set; }

        // NavegaciÃ³n
        public virtual ICollection<OperacionRemesaSEPA> Operaciones { get; set; } = new List<OperacionRemesaSEPA>();

        // Propiedades calculadas
        [NotMapped]
        public string TipoFicheroISO => Tipo == TipoRemesaSEPA.Transferencia ? "pain.001.001.03" : "pain.008.001.02";
    }

    /// <summary>
    /// OperaciÃ³n individual dentro de una remesa SEPA
    /// </summary>
    public class OperacionRemesaSEPA
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RemesaId { get; set; }
        [ForeignKey(nameof(RemesaId))]
        public virtual RemesaSEPA? Remesa { get; set; }

        public int Orden { get; set; } // Orden en fichero XML (1, 2, 3...)

        // Para transferencias (SCT)
        [StringLength(140)]
        public string? BeneficiarioNombre { get; set; }
        [StringLength(34)]
        public string? BeneficiarioIBAN { get; set; }
        [StringLength(11)]
        public string? BeneficiarioBIC { get; set; }
        [StringLength(70)]
        public string? BeneficiarioCalle { get; set; }
        [StringLength(16)]
        public string? BeneficiarioNumero { get; set; }
        [StringLength(16)]
        public string? BeneficiarioCodigoPostal { get; set; }
        [StringLength(35)]
        public string? BeneficiarioPoblacion { get; set; }
        [StringLength(2)]
        public string? BeneficiarioPais { get; set; } = "ES";

        // Para adeudos (SDD) - datos deudor
        [StringLength(140)]
        public string? DeudorNombre { get; set; }
        [StringLength(34)]
        public string? DeudorIBAN { get; set; }
        [StringLength(11)]
        public string? DeudorBIC { get; set; }
        public int? MandatoId { get; set; }
        [ForeignKey(nameof(MandatoId))]
        public virtual MandatoSEPA? Mandato { get; set; }
        [StringLength(35)]
        public string? ReferenciaUnicaMandato { get; set; } // RUM
        public TipoSecuenciaMandato TipoSecuencia { get; set; } = TipoSecuenciaMandato.RCUR;

        // Importe y concepto
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Importe { get; set; }

        [Required]
        [StringLength(3)]
        public string Moneda { get; set; } = "EUR";

        [StringLength(140)]
        public string? Concepto { get; set; } // MÃX 140 chars en SDD

        [StringLength(140)]
        public string? ReferenciaPropia { get; set; } // EndToEndId (transferencia) / MandateRelatedInfo (adeudo)

        // Referencia a documento origen en ERP
        public string? OrigenTipo { get; set; } // "Factura", "Nomina", "FacturaProveedor", "AdeudoCliente"
        public int? OrigenId { get; set; }

        // Estado individual
        public EstadoOperacionRemesa Estado { get; set; } = EstadoOperacionRemesa.Pendiente;

        // Resultado banco
        public string? CodigoRespuestaBanco { get; set; }
        public string? DescripcionRespuestaBanco { get; set; }
        public DateTime? FechaRespuestaBanco { get; set; }

        // DevoluciÃ³n / Rechazo
        public bool EsDevolucion { get; set; } = false;
        public string? CodigoDevolucion { get; set; } // Ej: "AM04" (fondos insuficientes), "MD01" (mandato invÃ¡lido)
        public DateTime? FechaDevolucion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>
    /// Extracto bancario / conciliaciÃ³n (camt.053 / camt.054)
    /// </summary>
    public class ExtractoBancario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CuentaBancariaId { get; set; }
        [ForeignKey(nameof(CuentaBancariaId))]
        public virtual CuentaBancaria? CuentaBancaria { get; set; }

        [Required]
        [StringLength(35)]
        public string ReferenciaExtracto { get; set; } = string.Empty; // ID del extracto del banco

        [Required]
        public DateTime FechaExtracto { get; set; } // Fecha del extracto (cierre dÃ­a)

        [Required]
        public DateTime FechaValor { get; set; } // Fecha valor movimientos

        // Saldos
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoInicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoFinal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCargos { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAbonos { get; set; }

        // Fichero original
        public string? XmlOriginal { get; set; } // camt.053
        public string? HashXmlSHA256 { get; set; }

        public DateTime FechaRecepcion { get; set; } = DateTime.Now;
        public bool Procesado { get; set; } = false;
        public DateTime? FechaProcesado { get; set; }

        // NavegaciÃ³n
        public virtual ICollection<MovimientoExtracto> Movimientos { get; set; } = new List<MovimientoExtracto>();
    }

    /// <summary>
    /// Movimiento individual en extracto bancario
    /// </summary>
    public class MovimientoExtracto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExtractoId { get; set; }
        [ForeignKey(nameof(ExtractoId))]
        public virtual ExtractoBancario? Extracto { get; set; }

        public int Secuencia { get; set; }

        [Required]
        public DateTime FechaValor { get; set; }
        public DateTime? FechaContable { get; set; }

        public TipoMovimientoBancario Tipo { get; set; } // Cargo / Abono

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Importe { get; set; }

        [Required]
        [StringLength(3)]
        public string Moneda { get; set; } = "EUR";

        [StringLength(140)]
        public string? Concepto { get; set; }

        [StringLength(140)]
        public string? ContrapartidaNombre { get; set; }

        [StringLength(34)]
        public string? ContrapartidaIBAN { get; set; }

        [StringLength(35)]
        public string? ReferenciaBanco { get; set; } // EntryReference / InstructionId

        [StringLength(35)]
        public string? EndToEndId { get; set; }

        [StringLength(35)]
        public string? MandateId { get; set; } // Para adeudos

        // ConciliaciÃ³n automÃ¡tica
        public bool Conciliado { get; set; } = false;
        public int? AsientoContableId { get; set; } // VinculaciÃ³n a contabilidad
        public int? OperacionRemesaId { get; set; } // VinculaciÃ³n a remesa enviada
        public int? FacturaId { get; set; } // VinculaciÃ³n a factura
        public DateTime? FechaConciliacion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public enum TipoRemesaSEPA
    {
        Transferencia = 0,  // SCT / SCT Inst
        Adeudo = 1          // SDD Core / SDD B2B
    }

    public enum EsquemaRemesaSEPA
    {
        SCT = 0,        // SEPA Credit Transfer
        SCT_Inst = 1,   // SEPA Instant Credit Transfer
        SDD_Core = 2,   // SEPA Direct Debit Core
        SDD_B2B = 3     // SEPA Direct Debit B2B
    }

    public enum EstadoRemesaSEPA
    {
        Borrador = 0,
        Validada = 1,
        Enviada = 2,
        AceptadaBanco = 3,
        ParcialmenteRechazada = 4,
        Rechazada = 5,
        Ejecutada = 6,
        Conciliada = 7
    }

    public enum EstadoOperacionRemesa
    {
        Pendiente = 0,
        Enviada = 1,
        Aceptada = 2,
        Rechazada = 3,
        Devuelta = 4,
        Ejecutada = 5
    }

    public enum TipoMovimientoBancario
    {
        Cargo = 0,    // Salida dinero (adeudo, transferencia emitida)
        Abono = 1     // Entrada dinero (transferencia recibida, adeudo cobrado)
    }
}