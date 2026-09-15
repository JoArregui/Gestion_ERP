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

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        /// <summary>
        /// Obtiene la empresa de la sesión (encapsulación: nadie lista el registro completo).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<Empresa>());
            var empresas = await _context.Empresas
                .AsNoTracking()
                .Where(e => e.IsActiva && e.Id == empresaId)
                .ToListAsync();

            return Ok(empresas);
        }

        /// <summary>
        /// Crea la primera empresa durante el onboarding inicial.
        /// Reservado al usuario inicial (admin@erp.local, sin rol).
        /// </summary>
        [Authorize]
        [HttpPost("crear-onboarding")]
        public async Task<ActionResult<Empresa>> CrearParaOnboarding([FromBody] string nombreEmpresa)
        {
            if (!ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Solo el usuario inicial puede crear la empresa del primer onboarding." });
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

            // El usuario inicial (admin@erp.local) NO se vincula nunca: sigue vacío.
            // Solo se vincula un usuario real autenticado que aún no tenga empresa.
            try
            {
                ApplicationUser? targetUser = null;
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                        targetUser = await _userManager.FindByIdAsync(userId);
                }

                if (targetUser != null
                    && !ERP.Domain.Constants.BootstrapUser.IsBootstrap(targetUser.Email)
                    && !ERP.Domain.Constants.BootstrapUser.IsBootstrap(targetUser.UserName)
                    && targetUser.EmpresaId == null)
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
            // Encapsulación por sesión: cada usuario solo ve su empresa
            if (id != GetEmpresaId()) return Forbid();
            var empresa = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

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

                // El usuario inicial (admin@erp.local) NO se vincula nunca: sigue vacío.
                // Solo se vincula un usuario real que aún no tenga empresa.
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var u = await _userManager.FindByIdAsync(userId);
                        if (u != null
                            && !ERP.Domain.Constants.BootstrapUser.IsBootstrap(u.Email)
                            && !ERP.Domain.Constants.BootstrapUser.IsBootstrap(u.UserName)
                            && u.EmpresaId == null)
                        {
                            u.EmpresaId = empresa.Id;
                            await _userManager.UpdateAsync(u);
                        }
                    }
                }
                catch { }

                return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
            }
            catch
            {
                // Sin Details: no se filtran mensajes técnicos/SQL al cliente.
                return BadRequest(new { Message = "Error al crear la entidad" });
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
            if (id != GetEmpresaId()) return Forbid();

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
            if (id != GetEmpresaId()) return Forbid();
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