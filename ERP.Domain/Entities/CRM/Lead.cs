using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.CRM
{
    /// <summary>
    /// Lead comercial (Fase B flujo ERP.txt §2). Contacto aún no cliente.
    /// Conversión: Lead → Cliente (+ Oportunidad inicial).
    /// </summary>
    public class Lead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required(ErrorMessage = "El nombre del contacto es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(150)]
        public string? EmpresaNombre { get; set; }

        [StringLength(20)]
        public string? CIF { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        public OrigenLead Origen { get; set; } = OrigenLead.Web;

        public EstadoLead Estado { get; set; } = EstadoLead.Nuevo;

        [StringLength(100)]
        public string? UsuarioAsignado { get; set; }

        public DateTime? FechaContacto { get; set; }

        [StringLength(1000)]
        public string? Notas { get; set; }

        // Conversión (trazabilidad, no borrado)
        public int? ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public virtual Cliente? Cliente { get; set; }

        public DateTime? FechaConversion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public enum OrigenLead
    {
        Web = 0,
        Telefono = 1,
        Feria = 2,
        Referido = 3,
        Telemarketing = 4,
        Otro = 5
    }

    public enum EstadoLead
    {
        Nuevo = 0,
        Contactado = 1,
        Cualificado = 2,
        Descartado = 3,
        Convertido = 4
    }
}
