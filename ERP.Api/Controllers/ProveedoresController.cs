using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Proveedores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Proveedor>>> Get()
        {
            return await _context.Proveedores
                .OrderBy(p => p.RazonSocial)
                .ToListAsync();
        }

        // GET: api/Proveedores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Proveedor>> Get(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return proveedor;
        }

        // POST: api/Proveedores
        [HttpPost]
        public async Task<ActionResult<Proveedor>> Post(Proveedor proveedor)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = proveedor.Id }, proveedor);
        }

        // PUT: api/Proveedores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Proveedor proveedor)
        {
            if (id != proveedor.Id) return BadRequest("El ID no coincide");

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
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();

            // En un ERP profesional, no solemos borrar físicamente para mantener trazabilidad contable
            proveedor.IsActivo = false; 
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}