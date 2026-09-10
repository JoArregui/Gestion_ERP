using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities;
using ERP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FamiliasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FamiliasController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.local", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.com", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.local", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.com", StringComparison.OrdinalIgnoreCase);

        // GET: api/Familias — RGPD: solo propias (genérico ve vacío)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Familia>>> GetFamilias()
        {
            try
            {
                if (IsGeneric) return Ok(new List<Familia>());
                var empresaId = GetEmpresaId();
                if (empresaId == 0) return Ok(new List<Familia>());
                return await _context.Familia
                    .Where(f => f.EmpresaId == empresaId)
                    .OrderBy(f => f.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al recuperar familias: {ex.Message}");
            }
        }

        // GET: api/Familias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Familia>> GetFamilia(int id)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var familia = await _context.Familia
                .Include(f => f.Articulos)
                .FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == empresaId);

            if (familia == null)
            {
                return NotFound(new { Message = "Familia no encontrada" });
            }

            return familia;
        }

        // POST: api/Familias
        [HttpPost]
        public async Task<ActionResult<Familia>> PostFamilia(Familia familia)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized("Sesión sin empresa — complete el onboarding.");
            try
            {
                familia.EmpresaId = empresaId;
                familia.FechaCreacion = DateTime.Now;
                _context.Familia.Add(familia);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFamilia), new { id = familia.Id }, familia);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al crear familia: {ex.Message}");
            }
        }

        // PUT: api/Familias/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFamilia(int id, Familia familia)
        {
            if (id != familia.Id) return BadRequest("El ID proporcionado no coincide con la entidad.");
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized();
            var existente = await _context.Familia.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == empresaId);
            if (existente == null)
            {
                return NotFound();
            }

            familia.FechaCreacion = existente.FechaCreacion;
            familia.UltimaModificacion = DateTime.Now;
            familia.EmpresaId = empresaId;

            _context.Entry(familia).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FamiliaExists(id)) return NotFound();
                else throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar: {ex.Message}");
            }

            return NoContent();
        }

        // DELETE: api/Familias/5 (Baja lógica con validación de integridad)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamilia(int id)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized();
            var familia = await _context.Familia.FirstOrDefaultAsync(f => f.Id == id && f.EmpresaId == empresaId);
            if (familia == null) return NotFound();
            // Verificar que pertenece a la empresa
            if (familia.EmpresaId != empresaId) return Forbid();

            // Validación de integridad: No desactivar si tiene artículos
            var tieneArticulos = await _context.Articulos.AnyAsync(a => a.FamiliaId == id);
            if (tieneArticulos)
            {
                return BadRequest("Restricción de integridad: No se puede desactivar una familia que contiene artículos vinculados. Mueva los artículos a otra categoría primero.");
            }

            familia.IsActiva = false;
            familia.UltimaModificacion = DateTime.Now;
            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FamiliaExists(int id)
        {
            return _context.Familia.Any(e => e.Id == id);
        }
    }
}