using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.RGPD
{
    /// <summary>Registro Actividades Tratamiento art.30 RGPD + LOPDGDD 3/2018</summary>
    public class RegistroTratamiento
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }
        [Required, StringLength(150)]
        public string NombreTratamiento { get; set; } = string.Empty; // Nóminas, Fichaje, Facturación...
        [Required]
        public string Finalidad { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string BaseJuridica { get; set; } = string.Empty; // 6.1.b Contrato, 6.1.c Obligación legal, 6.1.f Interés legítimo
        [StringLength(500)]
        public string? CategoriasInteresados { get; set; } // Empleados, Clientes
        [StringLength(500)]
        public string? CategoriasDatos { get; set; } // Identificativos, Económicos, Biométricos
        [StringLength(500)]
        public string? Destinatarios { get; set; }
        [StringLength(20)]
        public string PlazoConservacion { get; set; } = "4 años";
        public bool TransferenciasInternacionales { get; set; } = false;
        [StringLength(100)]
        public string? PaisDestino { get; set; }
        [StringLength(200)]
        public string? MecanismoSeguridad { get; set; }
        public string? MedidasTecnicas { get; set; }
        public bool RequiereEIPD { get; set; } = false;
        public DateTime? FechaEIPD { get; set; }
        public DateTime? FechaProximaRevision { get; set; }
        public DateTime? FechaUltimaRevision { get; set; }
        public bool TieneEncargadoTratamiento { get; set; } = false;
        [StringLength(100)]
        public string? EmailDelegado { get; set; }
        [StringLength(20)]
        public string? TelefonoDelegado { get; set; }
        [StringLength(100)]
        public string? DelegadoProteccionDatos { get; set; }
        public bool EsTratamientoOcasional { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>Liquidación Seguridad Social SLD: RLC/RNT (Orden PJC/297/2026)</summary>
    public class LiquidacionSeguridadSocial
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }
        [Required, StringLength(20)]
        public string CodigoCuentaCotizacion { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int Mes { get; set; }
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
        public EstadoLiquidacionSS Estado { get; set; } = EstadoLiquidacionSS.Borrador;
        public string? XmlRlcBase64 { get; set; } // Recibo Liquidación
        public string? XmlRntBase64 { get; set; } // Relación Nominal
        public string? CodigoRespuestaTGSS { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ImporteTotal { get; set; }
        public DateTime? FechaConfirmacion { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    public enum EstadoLiquidacionSS
    {
        Borrador = 0,
        Calculada = 1,
        Confirmada = 2,
        Ingresada = 3,
        Rectificada = 4,
        Anulada = 5
    }
}
