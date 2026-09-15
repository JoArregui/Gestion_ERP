using System.Security.Claims;

namespace ERP.Domain.Constants
{
    /// <summary>
    /// Usuario genérico ÚNICO de primera interacción en todo el proyecto.
    /// - Credenciales: admin@erp.local / Admin123!
    /// - SIN rol de administrador y SIN claims de permiso.
    /// - Lo único que puede hacer es el primer onboarding: crear la empresa
    ///   y el primer usuario asociado a esa empresa.
    /// - Cualquier otro endpoint le responde 403 (ver BootstrapOnlyOnboardingFilter en ERP.Api).
    /// </summary>
    public static class BootstrapUser
    {
        public const string Email = "admin@erp.local";
        public const string DefaultPassword = "Admin123!";
        public const string DisplayName = "Usuario inicial (solo onboarding)";

        public static bool IsBootstrap(string? email) =>
            string.Equals(email?.Trim(), Email, StringComparison.OrdinalIgnoreCase);

        public static bool IsBootstrapUser(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;
            return IsBootstrap(user.FindFirst(ClaimTypes.Email)?.Value)
                || IsBootstrap(user.FindFirst("email")?.Value)
                || IsBootstrap(user.FindFirst(ClaimTypes.Name)?.Value);
        }
    }
}
