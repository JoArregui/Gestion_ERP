using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmpresasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmpresasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Obtiene empresas visibles (RGPD + multi-empresa).
        /// - Genérico bootstrap → solo empresas sin usuarios (recién creadas para onboarding).
        /// - Usuario con N empresas → todas las asignadas.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
            if (user == null) return Unauthorized();
            if (IsGenericBootstrap(user))
            {
                // Bootstrap ve solo empresas huérfanas (sin usuarios) para poder asignar el primer usuario
                var todas = await _context.Empresas.Where(e => e.IsActiva).ToListAsync();
                var conUsuario = await _context.Users.Where(u => u.EmpresaId != null).Select(u => u.EmpresaId!.Value).ToListAsync();
                var conCruce = await _context.UserEmpresas.Select(ue => ue.EmpresaId).ToListAsync();
                var ocupadas = conUsuario.Concat(conCruce).Distinct().ToHashSet();
                var libres = todas.Where(e => !ocupadas.Contains(e.Id)).ToList();
                return Ok(libres);
            }
            var ids = new List<int>();
            if (user.EmpresaId.HasValue) ids.Add(user.EmpresaId.Value);
            var extras = await _context.UserEmpresas.Where(ue => ue.UserId == user.Id).Select(ue => ue.EmpresaId).ToListAsync();
            ids.AddRange(extras);
            ids = ids.Distinct().ToList();
            if (!ids.Any()) return Ok(new List<Empresa>());
            var list = await _context.Empresas.Where(e => e.IsActiva && ids.Contains(e.Id)).ToListAsync();
            return Ok(list);
        }

        private static bool IsGenericBootstrap(ApplicationUser u)
            => string.Equals(u.Email, "admin@erp.local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(u.Email, "admin@erp.com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(u.UserName, "admin@erp.local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(u.UserName, "admin@erp.com", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Crea la primera empresa durante el onboarding inicial
        /// </summary>
        [AllowAnonymous]
        [HttpPost("crear-onboarding")]
        public async Task<ActionResult<Empresa>> CrearParaOnboarding([FromBody] string nombreEmpresa)
        {
            if (string.IsNullOrWhiteSpace(nombreEmpresa))
            {
                return BadRequest(new { Message = "El nombre de la empresa es obligatorio" });
            }

            // Limpiar nombre: quitar caracteres especiales, tomar solo letras/números/guiones
            var nombreLimpio = string.Join("-", nombreEmpresa.Split(new[] { ' ', '/', '\\', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => string.Join("", s.Where(char.IsLetterOrDigit))));

            // Asegurar que tenga un formato coherente
            if (string.IsNullOrEmpty(nombreLimpio))
                nombreLimpio = "Empresa";

            var empresa = new Empresa
            {
                NombreComercial = nombreLimpio,
                RazonSocial = nombreEmpresa,
                CIF = $"B{Guid.NewGuid():N}"[..10],
                SerieFacturacion = DateTime.UtcNow.Year.ToString(),
                IvaDefecto = 21m,
                IsActiva = true,
                ColorHex = "#3498db",
                Eslogan = null,
                LogoUrl = null,
                LogoBase64 = null,
                FechaAlta = DateTime.UtcNow,
                UltimaModificacion = null,
                TerritorioFiscal = ERP.Domain.Entities.Fiscal.TerritorioFiscal.PeninsulaBaleares,
                EsSII = false
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            // Vinculación automática deshabilitada para el bootstrap genérico:
            // admin@erp.local / admin@erp.com es solo para el primer onboarding y NO debe quedar
            // asignado a ninguna empresa (debe permanecer vacío). Solo se vincula si es usuario real.
            try
            {
                ApplicationUser? targetUser = null;
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                        targetUser = await _userManager.FindByIdAsync(userId);
                }
                if (targetUser != null && !IsGenericBootstrap(targetUser) && targetUser.EmpresaId == null)
                {
                    targetUser.EmpresaId = empresa.Id;
                    await _userManager.UpdateAsync(targetUser);
                }
            }
            catch { }

            return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
        }

        /// <summary>
        /// Obtiene detalle — solo si el usuario pertenece a esa empresa (multi-tenant).
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Empresa>> GetEmpresa(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
            if (user == null) return Unauthorized();
            if (IsGenericBootstrap(user)) return Forbid();
            var ids = new List<int>();
            if (user.EmpresaId.HasValue) ids.Add(user.EmpresaId.Value);
            ids.AddRange(await _context.UserEmpresas.Where(ue => ue.UserId == user.Id).Select(ue => ue.EmpresaId).ToListAsync());
            if (!ids.Contains(id)) return Forbid();
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return NotFound(new { Message = "Empresa no encontrada" });
            return empresa;
        }

        /// <summary>
        /// Registra una nueva sede en el sistema
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Empresa>> PostEmpresa(Empresa empresa)
        {
            try
            {
                empresa.FechaAlta = DateTime.UtcNow;
                empresa.UltimaModificacion = null;
                
                _context.Empresas.Add(empresa);
                await _context.SaveChangesAsync();

                // Vinculación solo para usuarios reales, nunca para el bootstrap genérico
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var u = await _userManager.FindByIdAsync(userId);
                        if (u != null && !IsGenericBootstrap(u) && u.EmpresaId == null)
                        {
                            u.EmpresaId = empresa.Id;
                            await _userManager.UpdateAsync(u);
                        }
                    }
                }
                catch { }

                return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error al crear la entidad", Details = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza los datos de una entidad existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpresa(int id, Empresa empresa)
        {
            if (id != empresa.Id)
            {
                return BadRequest(new { Message = "El ID no coincide con la entidad" });
            }

            // Recuperamos la entidad original para no perder la FechaAlta
            var existente = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (existente == null)
            {
                return NotFound();
            }

            empresa.FechaAlta = existente.FechaAlta;
            empresa.UltimaModificacion = DateTime.UtcNow;

            _context.Entry(empresa).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmpresaExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Desactiva una empresa (Baja lógica) para preservar integridad referencial
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null)
            {
                return NotFound();
            }

            // Aplicamos baja lógica
            empresa.IsActiva = false;
            empresa.UltimaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmpresaExists(int id)
        {
            return _context.Empresas.Any(e => e.Id == id);
        }
    }
}