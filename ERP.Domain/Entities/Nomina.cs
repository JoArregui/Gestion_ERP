using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class Nomina
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpleadoId { get; set; }
        [ForeignKey("EmpleadoId")]
        public virtual Empleado? Empleado { get; set; }

        public int Mes { get; set; }
        public int Anio { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SalarioBase { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Complementos { get; set; }

        // Complementos desglosados (RD 1006/1995)
        [Column(TypeName = "decimal(18,4)")]
        public decimal HorasExtra { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal PagasExtraProrrateadas { get; set; }

        // Bases cotización SLD (Orden PJC/297/2026)
        [Column(TypeName = "decimal(18,4)")]
        public decimal BaseContingenciasComunes { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal BaseContingenciasProfesionales { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal BaseHorasExtra { get; set; }
        public int GrupoCotizacion { get; set; } = 7;
        [StringLength(20)]
        public string? CodigoCuentaCotizacion { get; set; } // CCC

        // Deducciones desglosadas
        [Column(TypeName = "decimal(18,4)")]
        public decimal CuotaSegSocialTrabajador { get; set; } // 4.70% CC + desempleo
        [Column(TypeName = "decimal(5,2)")]
        public decimal TipoIRPF { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal RetencionIRPF { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal Deducciones { get; set; } // legado: compatibilidad (si nuevos campos =0 se usa este)

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalNeto => SalarioBase + Complementos + HorasExtra + PagasExtraProrrateadas - (CuotaSegSocialTrabajador + RetencionIRPF > 0 ? CuotaSegSocialTrabajador + RetencionIRPF : Deducciones);

        // Propiedades para vista
        [Column(TypeName = "decimal(18,4)")]
        public decimal BaseTotal { get; set; } = 0m; // Suma de todas las bases

        [Column(TypeName = "decimal(18,4)")]
        public decimal CuotaSegSocialTrabajadorTotal { get; set; } = 0m; // Cuota completa

        // Propiedades para pagas extra
        public bool TienePagasExtra { get; set; } = false;
        public decimal ImportePagasExtra { get; set; } = 0m;
        public int NumeroPagasExtra { get; set; } = 0;

        public DateTime FechaEmision { get; set; } = DateTime.Now;
        
        public bool EstaPagada { get; set; } = false;

        // Vinculación remesa SEPA nóminas (pain.001)
        public int? RemesaSEPAId { get; set; }
        [ForeignKey(nameof(RemesaSEPAId))]
        public virtual Bancario.RemesaSEPA? RemesaSEPA { get; set; }

        // --- Campos SLD SEPA 2026 ---
        [StringLength(13)]
        public string NumeroSeguridadSocial { get; set; } = string.Empty; // 12-13 dígitos TGSS

        [StringLength(34)]
        public string IBAN { get; set; } = string.Empty; // Cuenta bancaria del trabajador

        public int DiasTrabajados { get; set; } = 0; // Días efectivamente trabajados en el período

        [StringLength(200)]
        public string ConceptoPago { get; set; } = "Nomina mensual"; // Descripción del pago

        [StringLength(2)]
        public string CodPais { get; set; } = "ES"; // ISO-2 país trabajador

        public DateTime FechaPago { get; set; } = DateTime.Now; // Fecha de pago efectiva

        public DateTime FechaValor { get; set; } = DateTime.Now; // Fecha de valor para banca

        public int? TipoContratoId { get; set; } // FK catálogo tipos contrato

        [StringLength(20)]
        public string? TipoContrato { get; set; } // 100 Indef, 402 Temporal, etc.
    }
}