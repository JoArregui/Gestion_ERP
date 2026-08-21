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

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
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
                // Actualizamos la propiedad que configuramos en ApplicationUser
                user.UltimoAcceso = DateTime.Now;
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
            
            // Claims básicos y personalizados para el ERP
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("FullName", user.FullName),
                // CLAIM DE TENANCY: Vital para filtrar datos por empresa en los servicios
                new Claim("EmpresaId", user.EmpresaId.ToString())
            };

            // Mapeo de roles a claims de seguridad
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
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