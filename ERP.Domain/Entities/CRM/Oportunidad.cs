using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.CRM
{
    /// <summary>
    /// Oportunidad de venta / embudo (Fase B flujo ERP.txt §2).
    /// Etapas: Prospeccion → Calificacion → Propuesta → Negociacion → Ganada/Perdida.
    /// Ganada → Presupuesto (DocumentoComercial Tipo Presupuesto).
    /// </summary>
    public class Oportunidad
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public int? LeadId { get; set; }
        [ForeignKey(nameof(LeadId))]
        public virtual Lead? Lead { get; set; }

        public int? ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public virtual Cliente? Cliente { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Range(0, 999999999)]
        public decimal ImporteEstimado { get; set; }

        [Range(0, 100)]
        public int Probabilidad { get; set; } = 10;

        public EtapaOportunidad Etapa { get; set; } = EtapaOportunidad.Prospeccion;

        public DateTime? FechaCierrePrevista { get; set; }
        public DateTime? FechaCierre { get; set; }

        [StringLength(500)]
        public string? MotivoPerdida { get; set; }

        [StringLength(1000)]
        public string? Notas { get; set; }

        // Presupuesto generado al ganar (trazabilidad ciclo comercial)
        public int? DocumentoPresupuestoId { get; set; }
        [ForeignKey(nameof(DocumentoPresupuestoId))]
        public virtual DocumentoComercial? DocumentoPresupuesto { get; set; }

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal ImportePonderado => Math.Round(ImporteEstimado * Probabilidad / 100m, 2);
    }

    public enum EtapaOportunidad
    {
        Prospeccion = 0,
        Calificacion = 1,
        Propuesta = 2,
        Negociacion = 3,
        Ganada = 4,
        Perdida = 5
    }
}
