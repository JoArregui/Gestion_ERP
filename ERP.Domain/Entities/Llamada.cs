using System;

namespace ERP.Domain.Entities
{
    public class Llamada
    {
        public int Id { get; set; }
        public int? EmpresaId { get; set; }
        public string Empresa { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Tiempo { get; set; } = string.Empty;
        public bool Urgente { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        // Propiedad de navegación opcional si usas la entidad Empresa
        public Empresa? EmpresaRelacion { get; set; }
    }
}