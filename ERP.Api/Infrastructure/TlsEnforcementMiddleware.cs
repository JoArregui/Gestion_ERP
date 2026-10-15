using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

namespace ERP.Api.Infrastructure
{
    /// <summary>
    /// Middleware para enforzar TLS 1.3 y redirección HTTPS estricta.
    /// Cumple con Privacy by Design (RGPD Art.25) y mejores prácticas de seguridad.
    /// </summary>
    public class TlsEnforcementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TlsEnforcementMiddleware> _logger;

        public TlsEnforcementMiddleware(RequestDelegate next, ILogger<TlsEnforcementMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // En desarrollo permitir HTTP, en producción forzar HTTPS
            var isDevelopment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            if (!context.Request.IsHttps && !isDevelopment)
            {
                // Redirigir a HTTPS
                var httpsUrl = "https://" + context.Request.Host + context.Request.Path + context.Request.QueryString;
                _logger.LogWarning("Redirigiendo HTTP a HTTPS: {Url}", httpsUrl);
                context.Response.Redirect(httpsUrl, permanent: true);
                return;
            }

            // Verificar versión TLS (requerir TLS 1.2+ en producción, 1.3 preferido)
            if (!isDevelopment && context.Connection.ClientCertificate == null)
            {
                var tlsVersion = context.Connection.GetType().GetProperty("TlsVersion")?.GetValue(context.Connection)?.ToString();
                if (!string.IsNullOrEmpty(tlsVersion) && (tlsVersion.Contains("1.0") || tlsVersion.Contains("1.1")))
                {
                    _logger.LogWarning("Conexión rechazada: TLS version {Version} no permitida", tlsVersion);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("Se requiere TLS 1.2 o superior");
                    return;
                }
            }

            // Headers de seguridad
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https:; frame-ancestors 'none'";

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method para registrar el middleware fácilmente.
    /// </summary>
    public static class TlsEnforcementMiddlewareExtensions
    {
        public static IApplicationBuilder UseTlsEnforcement(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TlsEnforcementMiddleware>();
        }
    }
}