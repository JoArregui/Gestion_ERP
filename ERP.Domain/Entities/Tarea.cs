using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.Entities
{
    public class Tarea
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Hora { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Descripcion { get; set; }

        [Required]
        [MaxLength(10)]
        public string Prioridad { get; set; } = "MEDIA";

        public bool Completada { get; set; }

        public int? EmpresaId { get; set; }

        public virtual Empresa? Empresa { get; set; }
    }
}