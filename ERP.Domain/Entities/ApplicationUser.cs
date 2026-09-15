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
        
        // ID de la empresa principal (para JWT y compatibilidad). Null = bootstrap sin empresa aún
        public int? EmpresaId { get; set; }
        public virtual Empresa? Empresa { get; set; }

        // Multi-empresa: usuario puede pertenecer a varias empresas
        public virtual ICollection<UserEmpresa> UserEmpresas { get; set; } = new List<UserEmpresa>();

        // --- 2º ONBOARDING (tutorial post-credenciales propias) ---
        // Se activa cuando el usuario entra por primera vez con credenciales propias (EmpresaId != null)
        // y aún no ha configurado los maestros básicos de la empresa.
        public bool SetupTutorialVisto { get; set; } = false;
        public bool SetupTutorialCompletado { get; set; } = false;
    }
}