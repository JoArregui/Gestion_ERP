using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User);

        // GET: api/Proveedores — RGPD solo propios (genérico vacío)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Proveedor>>> Get()
        {
            if (IsGeneric) return Ok(new List<Proveedor>());
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<Proveedor>());
            return await _context.Proveedores
                .Where(p => p.EmpresaId == empresaId)
                .OrderBy(p => p.RazonSocial)
                .ToListAsync();
        }

        // GET: api/Proveedores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Proveedor>> Get(int id)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (proveedor == null) return NotFound();
            return proveedor;
        }

        // POST: api/Proveedores
        [HttpPost]
        public async Task<ActionResult<Proveedor>> Post(Proveedor proveedor)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized("Sesión sin empresa.");
            proveedor.EmpresaId = empresaId;
            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = proveedor.Id }, proveedor);
        }

        // PUT: api/Proveedores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Proveedor proveedor)
        {
            if (id != proveedor.Id) return BadRequest("El ID no coincide");
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized();
            var existente = await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (existente == null) return NotFound();
            proveedor.EmpresaId = empresaId;
            _context.Entry(proveedor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Proveedores.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Proveedores/5 (Baja Lógica)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized();
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (proveedor == null) return NotFound();
            if (proveedor.EmpresaId != empresaId) return Forbid();

            // En un ERP profesional, no solemos borrar físicamente para mantener trazabilidad contable
            proveedor.IsActivo = false; 
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}