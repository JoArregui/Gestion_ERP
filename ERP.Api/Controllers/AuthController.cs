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
using Microsoft.EntityFrameworkCore;

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
        /// Flujo bifurcado:
        /// - Credenciales universales (admin@erp.local) -> solo BBDD INICIAL (erp.db) -> onboarding virgen
        /// - Credenciales privadas -> escanea todas las Gestion*.db existentes, exclusiva en la BBDD del usuario
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var emailNorm = model.Email?.Trim().ToLowerInvariant();
            var isUniversal = emailNorm == "admin@erp.local" || emailNorm == "admin@erp.com";

            ApplicationUser? user = null;
            string? tenantFileForUser = null;
            // Para privados: guardamos contexto tenant donde se encontró el usuario
            ERP.Data.ApplicationDbContext? tenantCtx = null;
            UserManager<ApplicationUser>? tenantUserManager = null;

            if (isUniversal)
            {
                // 1a. Universal -> solo BBDD INICIAL
                user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null) return Unauthorized(new { Message = "Credenciales incorrectas" });
                if (!user.IsActivo) return BadRequest(new { Message = "Su cuenta está desactivada. Contacte con el administrador." });
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded) return Unauthorized(new { Message = "Intento de inicio de sesión no autorizado" });
                user.UltimoAcceso = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                var tokenU = await GenerateJwtToken(user, tenantFile: null);
                return Ok(new { Token = tokenU, UserName = user.UserName, FullName = user.FullName, EmpresaId = user.EmpresaId });
            }
            else
            {
                // 1b. Privado -> escanear todas las Gestion*.db existentes
                var tenantService = HttpContext.RequestServices.GetService(typeof(ERP.Services.Tenant.TenantDatabaseService)) as ERP.Services.Tenant.TenantDatabaseService;
                var tenantFiles = tenantService?.ListTenantDatabases() ?? Array.Empty<string>();
                // Fallback: si ListTenantDatabases no devuelve nada, buscar físicamente Gestion*.db en el directorio del maestro
                if (tenantFiles.Length == 0)
                {
                    try
                    {
                        var masterConn = _configuration.GetConnectionString("DefaultConnection") ?? "Data Source=erp.db";
                        var mf = masterConn.Contains("Data Source=") ? masterConn.Split("Data Source=")[1].Split(';')[0].Trim() : "erp.db";
                        var baseDir = AppContext.BaseDirectory;
                        var masterPath = Path.IsPathRooted(mf) ? mf : Path.Combine(baseDir, mf);
                        var dir = Path.GetDirectoryName(masterPath) ?? baseDir;
                        if (Directory.Exists(dir))
                            tenantFiles = Directory.GetFiles(dir, "Gestion*.db").Select(Path.GetFileName).ToArray()!;
                    }
                    catch { }
                }

                var hasherDirect = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
                foreach (var tf in tenantFiles)
                {
                    if (string.IsNullOrWhiteSpace(tf)) continue;
                    var tenantPath = tenantService != null ? Path.Combine(Path.GetDirectoryName(tenantService.GetTenantDbPath("dummy")) ?? AppContext.BaseDirectory, tf) : Path.Combine(AppContext.BaseDirectory, tf);
                    if (!System.IO.File.Exists(tenantPath)) continue;
                    var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ERP.Data.ApplicationDbContext>();
                    opts.UseSqlite($"Data Source={tenantPath}");
                    var ctx = new ERP.Data.ApplicationDbContext(opts.Options);
                    var found = await ctx.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == model.Email.ToUpperInvariant() || u.Email == model.Email);
                    if (found == null) { ctx.Dispose(); continue; }
                    if (!found.IsActivo) { ctx.Dispose(); return BadRequest(new { Message = "Su cuenta está desactivada. Contacte con el administrador." }); }
                    var verify = hasherDirect.VerifyHashedPassword(found, found.PasswordHash ?? "", model.Password);
                    if (verify == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed) { ctx.Dispose(); continue; }
                    // Encontrado y password ok -> auditoría y token exclusivo en esta BBDD
                    found.UltimoAcceso = DateTime.UtcNow;
                    ctx.Users.Update(found);
                    await ctx.SaveChangesAsync();
                    user = found;
                    tenantFileForUser = tf;
                    tenantCtx = ctx;
                    // no UserManager needed for private; keep disposed later via tenantCtx only
                    break;
                }

                if (user == null) return Unauthorized(new { Message = "Credenciales incorrectas" });
                try
                {
                    var tokenP = await GenerateJwtToken(user, tenantFileForUser);
                    return Ok(new { Token = tokenP, UserName = user.UserName, FullName = user.FullName, EmpresaId = user.EmpresaId });
                }
                finally
                {
                    tenantCtx?.Dispose();
                }
            }
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

        private async Task<string> GenerateJwtToken(ApplicationUser user, string? tenantFile = null)
        {
            // Si tenantFile viene del scan privado ya lo tenemos; si no (universal), resolver desde maestro si hace falta
            string? resolvedTenant = tenantFile;
            if (resolvedTenant == null && user.EmpresaId.HasValue && user.EmpresaId.Value != 0)
            {
                var emp = await _context.Empresas.FindAsync(user.EmpresaId.Value);
                if (emp != null) resolvedTenant = ERP.Services.Tenant.TenantDatabaseService.GetTenantFileName(emp.RazonSocial ?? emp.NombreComercial);
            }
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _userManager.GetClaimsAsync(user);
            // Si el usuario vino de un tenant scan, los roles/permissions están en ese tenant, no en maestro
            if ((roles.Count == 0 && permissions.Count == 0) && !string.IsNullOrWhiteSpace(resolvedTenant))
            {
                try
                {
                    var ts = HttpContext.RequestServices.GetService(typeof(ERP.Services.Tenant.TenantDatabaseService)) as ERP.Services.Tenant.TenantDatabaseService;
                    var tp = ts != null ? Path.Combine(Path.GetDirectoryName(ts.GetTenantDbPath("dummy")) ?? AppContext.BaseDirectory, resolvedTenant) : Path.Combine(AppContext.BaseDirectory, resolvedTenant);
                    if (System.IO.File.Exists(tp))
                    {
                        var opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ERP.Data.ApplicationDbContext>();
                        opts.UseSqlite($"Data Source={tp}");
                        using var tctx = new ERP.Data.ApplicationDbContext(opts.Options);
                        var tu = await tctx.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                        if (tu != null)
                        {
                            var roleIds = await tctx.UserRoles.Where(ur => ur.UserId == tu.Id).Select(ur => ur.RoleId).ToListAsync();
                            var roleNames = await tctx.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!).ToListAsync();
                            roles = roleNames;
                            var userClaims = await tctx.UserClaims.Where(c => c.UserId == tu.Id && c.ClaimType == "Permission").ToListAsync();
                            permissions = userClaims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!)).ToList();
                            // Añadir también permisos de rol
                            foreach (var rid in roleIds)
                            {
                                var rc = await tctx.RoleClaims.Where(rc2 => rc2.RoleId == rid && rc2.ClaimType == "Permission").ToListAsync();
                                foreach (var rcc in rc) permissions.Add(new Claim(rcc.ClaimType!, rcc.ClaimValue!));
                            }
                        }
                    }
                }
                catch { }
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
            if (!string.IsNullOrWhiteSpace(resolvedTenant)) claims.Add(new Claim("Tenant", resolvedTenant));

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