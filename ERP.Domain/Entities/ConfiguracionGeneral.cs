using System;
using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.Entities
{
    public class ConfiguracionGeneral
    {
        [Key]
        public string Clave { get; set; } = string.Empty; // Ejemplo: "SMTP_Server"
        
        [Required]
        public string Valor { get; set; } = string.Empty; 

        // Añadida para compatibilidad con el SeedService
        public string? Descripcion { get; set; }
        
        public DateTime UltimaModificacion { get; set; } = DateTime.Now;
    }
}