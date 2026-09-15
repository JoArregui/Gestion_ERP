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
    public class AcreedoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AcreedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User);

        // GET: api/Acreedores — RGPD solo propios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Acreedor>>> Get()
        {
            if (IsGeneric) return Ok(new List<Acreedor>());
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<Acreedor>());
            return await _context.Acreedores
                .Where(a => a.IsActivo && a.EmpresaId == empresaId)
                .OrderBy(a => a.RazonSocial)
                .ToListAsync();
        }

        // GET: api/Acreedores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Acreedor>> Get(int id)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var acreedor = await _context.Acreedores.FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
            if (acreedor == null) return NotFound();
            return acreedor;
        }

        // POST: api/Acreedores
        [HttpPost]
        public async Task<ActionResult<Acreedor>> Post(Acreedor acreedor)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized("Sesión sin empresa.");
            acreedor.EmpresaId = empresaId;
            _context.Acreedores.Add(acreedor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = acreedor.Id }, acreedor);
        }

        // PUT: api/Acreedores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Acreedor acreedor)
        {
            if (id != acreedor.Id) return BadRequest("El ID no coincide");
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized();
            var existente = await _context.Acreedores.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
            if (existente == null) return NotFound();
            acreedor.EmpresaId = empresaId;
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
            var empresaId = GetEmpresaId();
            if (empresaId == 0 || IsGeneric) return Unauthorized();
            var acreedor = await _context.Acreedores.FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
            if (acreedor == null) return NotFound();
            if (acreedor.EmpresaId != empresaId) return Forbid();

            // En un ERP profesional, no solemos borrar físicamente para mantener trazabilidad contable
            acreedor.IsActivo = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}