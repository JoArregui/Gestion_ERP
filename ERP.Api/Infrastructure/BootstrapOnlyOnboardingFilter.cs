using ERP.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Api.Infrastructure
{
    /// <summary>
    /// El usuario genérico de primera interacción (admin@erp.local) NO tiene rol
    /// de administrador: lo único que puede hacer es el primer onboarding
    /// (ver/crear la empresa y crear el primer usuario).
    /// Cualquier otro endpoint autenticado le responde 403.
    /// Las llamadas anónimas (login, swagger) no se ven afectadas.
    /// </summary>
    public sealed class BootstrapOnlyOnboardingFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (BootstrapUser.IsBootstrapUser(context.HttpContext.User))
            {
                var path = (context.HttpContext.Request.Path.Value ?? string.Empty).TrimEnd('/').ToLowerInvariant();
                var method = context.HttpContext.Request.Method.ToUpperInvariant();
                var allowed =
                    (method == "GET" && path == "/api/empresas") ||
                    (method == "POST" && path == "/api/empresas") ||
                    (method == "POST" && path == "/api/empresas/crear-onboarding") ||
                    (method == "GET" && path == "/api/users") ||
                    (method == "POST" && path == "/api/users");
                if (!allowed)
                {
                    context.Result = new ObjectResult(new { Message = "El usuario inicial solo puede completar el onboarding: crear la empresa y el primer usuario." })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
            await next();
        }
    }
}
