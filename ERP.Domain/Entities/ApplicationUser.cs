using Microsoft.AspNetCore.Identity;
using System;

namespace ERP.Domain.Entities
{
    /// <summary>
    /// Extensión de IdentityUser adaptada para el sistema ERP Industrial.
    /// Incluye soporte para Multi-tenancy y perfiles de usuario extendidos.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Coincide con el UsersController (FullName)
        public string FullName { get; set; } = string.Empty;

        // Coincide con el UsersController (IsActivo)
        public bool IsActivo { get; set; } = true;

        // Auditoría real para el campo LastLogin del controlador
        public DateTime? UltimoAcceso { get; set; }

        // --- CONFIGURACIÓN MULTI-TENANCY ---
        
        // ID de la empresa a la que pertenece el usuario (null = bootstrap sin empresa aún)
        public int? EmpresaId { get; set; }
        
        // Propiedad de navegación virtual para permitir Lazy Loading si fuera necesario
        public virtual Empresa? Empresa { get; set; }
    }
}