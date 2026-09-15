using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

namespace ERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ERP.Api.Services.IEmailService? _emailService;
        private readonly ILogger<AuthController>? _logger;
        private readonly ERP.Data.MasterDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ERP.Data.MasterDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailService = serviceProvider.GetService<ERP.Api.Services.IEmailService>();
            _logger = serviceProvider.GetService<ILogger<AuthController>>();
            _context = context;
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

                // 4. Generar Token JWT con Claims profesionales (multi-empresa)
                var token = await GenerateJwtToken(user);
                var allEmpresas = await GetAllEmpresaIdsForUser(user);

                return Ok(new 
                { 
                    Token = token,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    EmpresaId = user.EmpresaId,
                    EmpresaIds = allEmpresas
                });
            }

            return Unauthorized(new { Message = "Intento de inicio de sesión no autorizado" });
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("switch-empresa/{empresaId}")]
        public async Task<IActionResult> SwitchEmpresa(int empresaId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            if (IsGenericBootstrap(user)) return Forbid();
            var all = await GetAllEmpresaIdsForUser(user);
            if (!all.Contains(empresaId)) return Forbid();
            // Actualizar EmpresaId principal al seleccionado (persistir para próximos logins)
            user.EmpresaId = empresaId;
            await _userManager.UpdateAsync(user);
            var token = await GenerateJwtToken(user);
            return Ok(new { Token = token, EmpresaId = empresaId, EmpresaIds = all });
        }

        private async Task<List<int>> GetAllEmpresaIdsForUser(ApplicationUser user)
        {
            if (IsGenericBootstrap(user)) return new List<int>();
            var ids = new List<int>();
            if (user.EmpresaId.HasValue) ids.Add(user.EmpresaId.Value);
            var extras = await _context.UserEmpresas.Where(ue => ue.UserId == user.Id).Select(ue => ue.EmpresaId).ToListAsync();
            ids.AddRange(extras);
            return ids.Distinct().ToList();
        }

        private static bool IsGenericBootstrap(ApplicationUser user) =>
            ERP.Domain.Constants.BootstrapUser.IsBootstrap(user.Email)
            || ERP.Domain.Constants.BootstrapUser.IsBootstrap(user.UserName);

        public class ForgotPasswordRequest { public string Email { get; set; } = string.Empty; }
        public class ResetPasswordRequest { public string Email { get; set; } = string.Empty; public string Token { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; public string ConfirmPassword { get; set; } = string.Empty; }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Email)) return BadRequest(new { Message = "Email requerido" });
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            // Respuesta genérica para no revelar si el email existe (seguridad)
            var genericOk = Ok(new { Message = "Si el correo existe, recibirás instrucciones para restablecer tu contraseña." });
            if (user == null) return genericOk;
            if (!user.IsActivo) return genericOk;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Token debe ser URL-safe para el link
            var encodedToken = System.Net.WebUtility.UrlEncode(token);

            // Intentar envío por email si está configurado; si no, lo devolvemos en modo desarrollo
            var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?email={System.Net.WebUtility.UrlEncode(user.Email!)}&token={encodedToken}";
            var devMode = string.IsNullOrEmpty(_configuration["EmailSettings:SmtpServer"]) || _configuration["EmailSettings:SmtpServer"] == "smtp.gmail.com";
            try
            {
                if (_emailService != null && !devMode)
                {
                    var html = $"<p>Hola {user.FullName ?? user.Email},</p><p>Has solicitado restablecer tu contraseña.</p><p><a href=\"{resetLink}\">Haz clic aquí para crear una nueva contraseña</a></p><p>Si no fuiste tú, ignora este correo.</p><p>Token: {token}</p>";
                    await _emailService.SendEmailAsync(user.Email!, "Restablecer contraseña - ERP", html, null!);
                    _logger?.LogInformation("ForgotPassword email enviado a {Email}", user.Email);
                }
                else
                {
                    _logger?.LogWarning("ForgotPassword dev token para {Email}: {Token} link {Link}", user.Email, token, resetLink);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error enviando email forgot-password a {Email}", model.Email);
            }

            // En desarrollo devolvemos el token para facilitar pruebas sin SMTP
            if (devMode)
                return Ok(new { Message = "Si el correo existe, recibirás instrucciones.", DevToken = token, DevLink = resetLink });

            return genericOk;
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(model.NewPassword))
                return BadRequest(new { Message = "Datos incompletos" });
            if (model.NewPassword != model.ConfirmPassword)
                return BadRequest(new { Message = "Las contraseñas no coinciden" });
            if (model.NewPassword.Length < 8)
                return BadRequest(new { Message = "La contraseña debe tener al menos 8 caracteres" });

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null) return BadRequest(new { Message = "Solicitud inválida" });

            // El token viene URL-encoded desde el link
            var decodedToken = System.Net.WebUtility.UrlDecode(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
            if (result.Succeeded)
            {
                // Opcional: desbloquear si estaba bloqueado
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Ok(new { Message = "Contraseña restablecida correctamente. Ya puedes iniciar sesión." });
            }
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = $"No se pudo restablecer: {errors}" });
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _userManager.GetClaimsAsync(user);
            
            var allEmps = await GetAllEmpresaIdsForUser(user);
            // Claims básicos — sub = Id para que MapInboundClaims no colisione con Email
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim("EmpresaId", (user.EmpresaId ?? 0).ToString())
            };
            // Multi-empresa: añadir todas como claims adicionales para que el backend pueda validar acceso a cualquiera
            foreach (var eid in allEmps.Where(id => id != (user.EmpresaId ?? 0)).Distinct())
                claims.Add(new Claim("EmpresaId", eid.ToString()));

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
