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
    public class AcreedoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AcreedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Acreedores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Acreedor>>> Get()
        {
            return await _context.Acreedores
                .AsNoTracking()
                .Where(a => a.IsActivo)
                .OrderBy(a => a.RazonSocial)
                .ToListAsync();
        }

        // GET: api/Acreedores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Acreedor>> Get(int id)
        {
            var acreedor = await _context.Acreedores.FindAsync(id);
            if (acreedor == null) return NotFound();
            return acreedor;
        }

        // POST: api/Acreedores
        [HttpPost]
        public async Task<ActionResult<Acreedor>> Post(Acreedor acreedor)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Acreedores.Add(acreedor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = acreedor.Id }, acreedor);
        }

        // PUT: api/Acreedores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Acreedor acreedor)
        {
            if (id != acreedor.Id) return BadRequest("El ID no coincide");

            _context.Entry(acreedor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Acreedores.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Acreedores/5 (Baja Lógica)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var acreedor = await _context.Acreedores.FindAsync(id);
            if (acreedor == null) return NotFound();

            // En un ERP profesional, no solemos borrar físicamente para mantener trazabilidad contable
            acreedor.IsActivo = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}