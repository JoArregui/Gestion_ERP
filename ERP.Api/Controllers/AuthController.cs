using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ERP.Domain.Entities;
using ERP.Domain.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using ERP.Api.Services;

namespace ERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ERP.Data.ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ERP.Data.ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Procesa el inicio de sesión, actualiza auditoría y genera el Token JWT con contexto de empresa.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Localizar usuario
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) 
                return Unauthorized(new { Message = "Credenciales incorrectas" });

            // 2. Validar estado en el CMS (Propiedad de ApplicationUser)
            if (!user.IsActivo)
            {
                return BadRequest(new { Message = "Su cuenta está desactivada. Contacte con el administrador." });
            }

            // 3. Validar Password
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (result.Succeeded)
            {
                // --- AUDITORÍA AUTOMÁTICA ---
                user.UltimoAcceso = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                // ----------------------------

                // 4. Generar Token JWT con Claims profesionales
                var token = await GenerateJwtToken(user);

                return Ok(new 
                { 
                    Token = token,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    EmpresaId = user.EmpresaId
                });
            }

            return Unauthorized(new { Message = "Intento de inicio de sesión no autorizado" });
        }

        /// <summary>
        /// Solicita recuperación de contraseña: genera token y envía correo con enlace de restablecimiento.
        /// Siempre responde 200 para no revelar si el email existe (seguridad).
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            // Respuesta genérica para no enumerar usuarios
            var genericOk = Ok(new { Message = "Si el correo existe en el sistema, recibirás un email con instrucciones para restablecer tu contraseña." });

            if (user == null) return genericOk;
            if (!user.IsActivo) return genericOk;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Codificar para URL segura
            var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // BaseUrl del front (configurable). Fallback a localhost:5053 (ERP.Web)
            var webBaseUrl = _configuration["WebApp:BaseUrl"] 
                ?? _configuration["ERP.Web:BaseUrl"] 
                ?? "http://localhost:5053";
            webBaseUrl = webBaseUrl.TrimEnd('/');
            var resetLink = $"{webBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(tokenEncoded)}";

            var html = $@"
                <div style='font-family:sans-serif;max-width:600px;margin:auto;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden'>
                  <div style='background:#0f172a;color:white;padding:24px;text-align:center'>
                    <div style='width:48px;height:48px;background:#2563eb;border-radius:12px;display:inline-flex;align-items:center;justify-content:center;font-weight:900;font-size:20px'>E</div>
                    <h2 style='margin:12px 0 0;text-transform:uppercase;letter-spacing:1px;font-size:16px'>Recuperar contraseña — ERP PYMES 2026</h2>
                  </div>
                  <div style='padding:24px;color:#334155;line-height:1.6'>
                    <p>Hola <b>{System.Net.WebUtility.HtmlEncode(user.FullName ?? user.Email)}</b>,</p>
                    <p>Has solicitado restablecer tu contraseña para <b>{System.Net.WebUtility.HtmlEncode(user.Email)}</b>.</p>
                    <p>Haz clic en el siguiente botón (válido 2 horas):</p>
                    <p style='text-align:center;margin:24px 0'>
                      <a href='{resetLink}' style='background:#2563eb;color:white;padding:12px 28px;border-radius:10px;text-decoration:none;font-weight:900;font-size:12px;letter-spacing:0.1em;text-transform:uppercase;display:inline-block'>Restablecer contraseña</a>
                    </p>
                    <p style='font-size:12px;color:#64748b'>Si no solicitaste este cambio, ignora este correo. Tu contraseña actual seguirá siendo válida.</p>
                    <p style='font-size:11px;color:#94a3b8;word-break:break-all'>Enlace alternativo:<br/><a href='{resetLink}'>{resetLink}</a></p>
                  </div>
                  <div style='background:#f8fafc;padding:12px;text-align:center;font-size:11px;color:#94a3b8'>&copy; 2026 ERP Industrial — {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</div>
                </div>";

            var sent = false;
            try
            {
                sent = await _emailService.SendEmailAsync(user.Email!, "Recuperar contraseña — ERP", html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email de recuperación a {Email}", user.Email);
            }

            if (!sent)
            {
                _logger.LogWarning("No se pudo enviar email a {Email}. Token (solo desarrollo): {Token}", user.Email, tokenEncoded);
                // En desarrollo, devolver token para facilitar pruebas si el SMTP no está configurado
                if (_configuration.GetValue<bool>("EmailSettings:ReturnTokenInDev") || 
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    return Ok(new { Message = "Si el correo existe en el sistema, recibirás un email con instrucciones para restablecer tu contraseña.", DevToken = tokenEncoded, DevLink = resetLink });
                }
            }

            return genericOk;
        }

        /// <summary>
        /// Restablece la contraseña con el token recibido por email.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { Message = "Solicitud no válida." });

            string tokenDecoded;
            try
            {
                tokenDecoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            }
            catch
            {
                return BadRequest(new { Message = "Token no válido." });
            }

            var result = await _userManager.ResetPasswordAsync(user, tokenDecoded, model.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Contraseña restablecida para {Email}", user.Email);
                return Ok(new { Message = "Contraseña restablecida correctamente. Ya puedes iniciar sesión." });
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = errors });
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _userManager.GetClaimsAsync(user);
            
            // Resolver Tenant file desde BBDD INICIAL (erp.db) — registro maestro de TODOS los usuarios/empresas
            // El usuario existe duplicado en erp.db (maestro) y en GestionX.db; el login siempre consulta erp.db
            // porque en ese momento aún no hay claim Tenant, por lo que ApplicationDbContext apunta al maestro.
            string? tenantFile = null;
            if (user.EmpresaId.HasValue && user.EmpresaId.Value != 0)
            {
                var emp = await _context.Empresas.FindAsync(user.EmpresaId.Value);
                if (emp != null) tenantFile = ERP.Services.Tenant.TenantDatabaseService.GetTenantFileName(emp.RazonSocial ?? emp.NombreComercial);
            }

            // Claims básicos y personalizados para el ERP
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("FullName", user.FullName ?? string.Empty),
                // CLAIM DE TENANCY: Vital para filtrar datos por empresa en los servicios (0 si bootstrap sin empresa)
                new Claim("EmpresaId", (user.EmpresaId ?? 0).ToString())
            };
            if (!string.IsNullOrWhiteSpace(tenantFile)) claims.Add(new Claim("Tenant", tenantFile));

            // Mapeo explícito de roles a claims de seguridad
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                // Duplicamos el claim con la clave 'role' explícita para evitar inconsistencias de desmaterialización en Blazor WASM
                claims.Add(new Claim("role", role));
            }

            foreach (var permission in permissions.Where(c => c.Type == "Permission"))
            {
                claims.Add(new Claim("Permission", permission.Value));
            }

            // Usamos la clave definida en tu appsettings.json
            var jwtSecret = _configuration["JWT:Secret"];
            if (string.IsNullOrEmpty(jwtSecret)) 
                throw new Exception("La clave secreta JWT no está configurada en appsettings.json");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"] ?? "ERP.Api",
                audience: _configuration["JWT:Audience"] ?? "ERP.Web",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}