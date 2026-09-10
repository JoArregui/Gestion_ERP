using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Services; // Para IEmailService
using System.Threading.Tasks;
using ERP.Api.Services;

namespace ERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public SettingsController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Obtiene la configuración de email actual. 
        /// Si no existe, devuelve un objeto con valores por defecto.
        /// </summary>
        [HttpGet("email")]
        public async Task<ActionResult<EmailConfigDto>> GetEmailSettings()
        {
            // Buscamos en una tabla de configuración genérica o devolvemos defaults
            // Aquí asumo que podrías tener una tabla de Config o lo manejas por constantes
            var config = await _context.Set<ConfiguracionGeneral>()
                .FirstOrDefaultAsync(c => c.Clave == "SMTP_CONFIG");

            if (config == null)
            {
                return Ok(new EmailConfigDto());
            }

            // Aquí deberías deserializar el JSON de la base de datos (System.Text.Json)
            var dto = System.Text.Json.JsonSerializer.Deserialize<EmailConfigDto>(config.Valor);
            return Ok(dto);
        }

        /// <summary>
        /// Guarda o actualiza la configuración SMTP en la base de datos
        /// </summary>
        [HttpPost("email")]
        public async Task<IActionResult> SaveEmailSettings(EmailConfigDto dto)
        {
            var jsonValor = System.Text.Json.JsonSerializer.Serialize(dto);
            var config = await _context.Set<ConfiguracionGeneral>()
                .FirstOrDefaultAsync(c => c.Clave == "SMTP_CONFIG");

            if (config == null)
            {
                _context.Set<ConfiguracionGeneral>().Add(new ConfiguracionGeneral 
                { 
                    Clave = "SMTP_CONFIG", 
                    Valor = jsonValor,
                    UltimaModificacion = System.DateTime.Now 
                });
            }
            else
            {
                config.Valor = jsonValor;
                config.UltimaModificacion = System.DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Realiza un envío de prueba sin guardar los cambios permanentemente
        /// </summary>
        [HttpPost("email-test")]
        public async Task<IActionResult> TestEmailConnection(EmailConfigDto dto)
        {
            try
            {
                // Intentamos enviar un correo de prueba usando los datos recibidos del formulario
                bool enviado = await _emailService.SendEmailAsync(
                    dto.Username, // Se lo enviamos al mismo usuario que configura
                    "Prueba de Configuración ERP",
                    $"<h1>Conexión Exitosa</h1><p>El sistema ERP ha verificado la configuración SMTP de {dto.SenderName}.</p>",
                    dto // Pasamos el DTO para que el servicio use estos datos y no los de la DB
                );

                if (enviado) return Ok();
                return BadRequest("El servidor SMTP rechazó la conexión.");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error de protocolo SMTP: {ex.Message}");
            }
        }

        // Clase DTO para transferencia de datos
        public class EmailConfigDto
        {
            public string SmtpServer { get; set; } = "smtp.gmail.com";
            public int Port { get; set; } = 587;
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public bool EnableSsl { get; set; } = true;
            public bool UseDefaultCredentials { get; set; } = false;
            public string SenderName { get; set; } = "ERP Corporativo";
            public string SenderEmail { get; set; } = "";
        }
    }
}