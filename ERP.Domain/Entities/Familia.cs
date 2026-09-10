using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ERP.Domain.Entities
{
    public class Familia
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la familia es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "La descripción no puede superar los 255 caracteres")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Código visual o abreviatura para reportes (ej: "ALM" para Alimentación)
        /// </summary>
        [StringLength(10, ErrorMessage = "El código interno no puede superar los 10 caracteres")]
        public string? CodigoInterno { get; set; }

        public bool IsActiva { get; set; } = true;

        // Relación inversa con Artículos
        // Se utiliza virtual para permitir Lazy Loading si el proxy está configurado
        [JsonIgnore]
        public virtual ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();

        // Propiedades de auditoría (opcionales pero recomendadas en ERP)
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? UltimaModificacion { get; set; }
    }
}