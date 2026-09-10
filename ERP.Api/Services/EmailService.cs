using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ERP.Domain.DTOs;
using System.Text;
using static ERP.Api.Controllers.SettingsController;

namespace ERP.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendEmailAsync(string destinatario, string asunto, string cuerpo, EmailConfigDto? configOverride = null)
        {
            try
            {
                // Prioridad: 1. Configuración manual (Override) -> 2. Configuración de appsettings.json
                var settings = _config.GetSection("EmailSettings");
                
                string smtpServer = configOverride?.SmtpServer ?? settings["SmtpServer"] ?? "";
                int port = configOverride?.Port ?? int.Parse(settings["Port"] ?? "587");
                string senderName = configOverride?.SenderName ?? settings["SenderName"] ?? "ERP System";
                string senderEmail = configOverride?.SenderEmail ?? settings["SenderEmail"] ?? "";
                string username = configOverride?.Username ?? settings["Username"] ?? "";
                string password = configOverride?.Password ?? settings["Password"] ?? "";

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(senderName, senderEmail));
                email.To.Add(MailboxAddress.Parse(destinatario));
                email.Subject = asunto;

                var bodyBuilder = new BodyBuilder { HtmlBody = cuerpo };
                email.Body = bodyBuilder.ToMessageBody();

                using var smtp = new SmtpClient();
                
                // Usamos StartTls por defecto si el puerto es 587
                var secureOptions = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                
                await smtp.ConnectAsync(smtpServer, port, secureOptions);
                await smtp.AuthenticateAsync(username, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // Loguear el error para debug
                Console.WriteLine($"[EMAIL ERROR]: {ex.Message}");
                return false;
            }
        }

        public async Task SendReporteAsync(EnvioReporteDTO reporte)
        {
            var html = new StringBuilder();
            html.Append("<div style='font-family: sans-serif; max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>");
            
            // Header estilo Industrial
            html.Append("<div style='background-color: #0f172a; color: white; padding: 20px; text-align: center;'>");
            html.Append($"<h2 style='margin: 0; text-transform: uppercase; letter-spacing: 1px;'>{reporte.Titulo}</h2>");
            html.Append("</div>");
            
            // Cuerpo
            html.Append("<div style='padding: 20px;'>");
            html.Append("<table style='width: 100%; border-collapse: collapse;'>");
            foreach (var item in reporte.Items)
            {
                html.Append("<tr style='border-bottom: 1px solid #f1f5f9; font-size: 13px;'>");
                html.Append($"<td style='padding: 10px; color: #1e293b; font-weight: bold;'>{item.Principal}</td>");
                html.Append($"<td style='padding: 10px; color: #64748b;'>{item.Secundario}</td>");
                html.Append($"<td style='padding: 10px; color: #e11d48; font-weight: bold; text-align: right;'>{item.Valor}</td>");
                html.Append("</tr>");
            }
            html.Append("</table></div>");
            
            html.Append("<div style='background-color: #f8fafc; padding: 15px; text-align: center; font-size: 11px; color: #94a3b8;'>");
            html.Append($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}</div></div>");

            await SendEmailAsync(reporte.Destinatario, $"📊 Reporte: {reporte.Titulo}", html.ToString());
        }
    }
}