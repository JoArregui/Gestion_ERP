using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.Entities
{
    public class Acreedor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El CIF/NIF es obligatorio")]
        [StringLength(20, ErrorMessage = "El CIF no puede superar los 20 caracteres")]
        public string CIF { get; set; } = string.Empty;

        [Required(ErrorMessage = "La Razón Social es obligatoria")]
        [StringLength(150, ErrorMessage = "La Razón Social no puede superar los 150 caracteres")]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El nombre de contacto es demasiado largo")]
        public string? NombreContacto { get; set; }

        [EmailAddress(ErrorMessage = "El formato del Email no es válido")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20)]
        public string? Telefono { get; set; }

        /// <summary>
        /// true: Acreedor (Servicios como Luz, Agua, Alquiler)
        /// false: Proveedor (Mercaderías para Stock)
        /// </summary>
        public bool EsAcreedor { get; set; }

        public bool IsActivo { get; set; } = true;

        // Auditoría básica
        public DateTime FechaAlta { get; set; } = DateTime.Now;
    }
}