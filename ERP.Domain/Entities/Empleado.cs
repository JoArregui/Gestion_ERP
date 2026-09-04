using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class Empleado
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El DNI/NIE es obligatorio")]
        [StringLength(20)]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Formato de email incorrecto")]
        public string? Email { get; set; }
        
        public string? Telefono { get; set; }

        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty; // Nombre + Apellidos

        [StringLength(20)]
        public string CCC { get; set; } = string.Empty; // Código Cuenta Cotización TGSS

        [Required(ErrorMessage = "El Nº de Seguridad Social es obligatorio")]
        public string NumeroSeguridadSocial { get; set; } = string.Empty;

        // --- ACCESO KIOSKO ---
        [Required(ErrorMessage = "El PIN de acceso es obligatorio para el fichaje")]
        [StringLength(10)]
        public string PinAcceso { get; set; } = string.Empty;

        // --- DATOS CONTRACTUALES ---
        public string? Cargo { get; set; } 
        public string? Departamento { get; set; } 
        public int GrupoCotizacion { get; set; } = 7; // 1-11
        [StringLength(20)]
        public string? CodigoCuentaCotizacion { get; set; } // CCC TGSS
        [StringLength(50)]
        public string? ConvenioColectivo { get; set; }
        [StringLength(20)]
        public string? TipoContrato { get; set; } // 100 Indef, 402 Temporal etc.
        [StringLength(20)]
        public string? CNAE { get; set; }
        [StringLength(12)]
        public string? NumeroAfiliacionNAF { get; set; }
        public DateTime? FechaAntiguedad { get; set; }
        
        [Required]
        public DateTime FechaAlta { get; set; } = DateTime.Now;
        public DateTime? FechaBaja { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SalarioBrutoAnual { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SalarioBaseMensual { get; set; }

        // --- PAGO Y BANCO ---
        [StringLength(34)]
        public string? IBAN { get; set; } 

        // --- NUEVOS CAMPOS: VACACIONES ---
        public int VacacionesTotales { get; set; } = 22;
        public int VacacionesDisfrutadas { get; set; } = 0;
        
        [NotMapped]
        public int VacacionesPendientes => VacacionesTotales - VacacionesDisfrutadas;

        // --- NUEVOS CAMPOS: DOCUMENTACIÓN ---
        public string? RutaDocumentoPdf { get; set; }
        public string? NombreArchivoPdf { get; set; }

        // --- LÓGICA DE ESTADO ---
        [NotMapped] 
        public bool IsActivo => !FechaBaja.HasValue || FechaBaja > DateTime.Now;

        // --- RELACIONES ---
        public int EmpresaId { get; set; }
        
        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        public virtual ICollection<Nomina> Nominas { get; set; } = new List<Nomina>();
        public virtual ICollection<ControlHorario> ControlesHorarios { get; set; } = new List<ControlHorario>();
    }
}