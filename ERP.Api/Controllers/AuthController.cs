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

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ERP.Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Procesa el inicio de sesión, actualiza auditoría y genera el Token JWT con contexto de empresa.
        /// </summary>
        [HttpPost("login")]
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

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _userManager.GetClaimsAsync(user);
            
            // Resolver Tenant file desde BBDD INICIAL (erp.db) - duplicado en GestionX
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