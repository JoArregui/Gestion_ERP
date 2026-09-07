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
        /// Obtiene el listado completo de empresas activas
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            var empresas = await _context.Empresas
                .Where(e => e.IsActiva)
                .ToListAsync();

            return Ok(empresas);
        }

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

            // Vincular automáticamente al usuario bootstrap (admin@erp.local) si aún no tiene empresa
            // o al usuario autenticado si lo hay
            try
            {
                ApplicationUser? targetUser = null;
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                        targetUser = await _userManager.FindByIdAsync(userId);
                }
                if (targetUser == null)
                    targetUser = await _userManager.FindByEmailAsync("admin@erp.local");

                if (targetUser != null && targetUser.EmpresaId == null)
                {
                    targetUser.EmpresaId = empresa.Id;
                    await _userManager.UpdateAsync(targetUser);
                }
            }
            catch { /* no bloquea la creación si falla la vinculación */ }

            return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
        }

        /// <summary>
        /// Obtiene el detalle de una empresa por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Empresa>> GetEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
            {
                return NotFound(new { Message = "Empresa no encontrada" });
            }

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

                // Si es la primera empresa y el usuario bootstrap aún no tiene EmpresaId, vincularla
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var u = await _userManager.FindByIdAsync(userId);
                        if (u != null && u.EmpresaId == null)
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