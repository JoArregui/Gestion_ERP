using ERP.Domain.DTOs;
using static ERP.Api.Controllers.SettingsController;

namespace ERP.Api.Services
{
    public interface IEmailService
    {
        // Para enviar los reportes industriales
        Task SendReporteAsync(EnvioReporteDTO reporte);
        
        // Para la prueba de conexión y envíos genéricos del sistema
        Task<bool> SendEmailAsync(string destinatario, string asunto, string cuerpo, EmailConfigDto? configOverride = null);
    }
}