using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities;
using ERP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FamiliasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FamiliasController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // GET: api/Familias
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Familia>>> GetFamilias()
        {
            try
            {
                // Pasillo universal (EmpresaId 0) → programa vacío
                if (GetEmpresaId() == 0) return Ok(new List<Familia>());
                return await _context.Familia
                    .AsNoTracking()
                    .OrderBy(f => f.Nombre)
                    .ToListAsync();
            }
            catch
            {
                return StatusCode(500, "Error al recuperar familias.");
            }
        }

        // GET: api/Familias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Familia>> GetFamilia(int id)
        {
            var familia = await _context.Familia
                .Include(f => f.Articulos)
                .FirstOrDefaultAsync(f => f.Id == id);

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                familia.FechaCreacion = DateTime.Now;
                _context.Familia.Add(familia);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFamilia), new { id = familia.Id }, familia);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error al crear familia.");
            }
        }

        // PUT: api/Familias/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFamilia(int id, Familia familia)
        {
            if (id != familia.Id)
            {
                return BadRequest("El ID proporcionado no coincide con la entidad.");
            }

            var existente = await _context.Familia.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (existente == null)
            {
                return NotFound();
            }

            familia.FechaCreacion = existente.FechaCreacion;
            familia.UltimaModificacion = DateTime.Now;

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
                return StatusCode(500, "Error al actualizar.");
            }

            return NoContent();
        }

        // DELETE: api/Familias/5 (Baja lógica con validación de integridad)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamilia(int id)
        {
            var familia = await _context.Familia.FindAsync(id);
            if (familia == null)
            {
                return NotFound();
            }

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