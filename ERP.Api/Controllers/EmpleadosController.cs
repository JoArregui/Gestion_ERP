using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using System.Security.Claims;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmpleadosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmpleadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User);

        // GET: api/Empleados — RGPD solo propios (genérico vacío)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empleado>>> GetEmpleados()
        {
            if (IsGeneric) return Ok(new List<Empleado>());
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<Empleado>());
            return await _context.Empleados
                .Where(e => e.EmpresaId == empresaId && e.FechaBaja == null)
                .OrderBy(e => e.Apellidos)
                .ToListAsync();
        }

        // GET: api/Empleados/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Empleado>> GetEmpleado(int id)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
            if (empleado == null) return NotFound();
            return empleado;
        }

        // POST: api/Empleados
        [HttpPost]
        public async Task<ActionResult<Empleado>> PostEmpleado(Empleado empleado)
        {
            if (IsGeneric) return Unauthorized("Genérico sin empresa no opera empleados.");
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Unauthorized("Sesión sin empresa.");
            try
            {
                empleado.Empresa = null!;
                empleado.EmpresaId = empresaId;
                if (empleado.FechaAlta == default) empleado.FechaAlta = DateTime.Now;
                empleado.FechaBaja = null;
                _context.Empleados.Add(empleado);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetEmpleado), new { id = empleado.Id }, empleado);
            }
            catch (Exception)
            {
                return BadRequest("Error al crear la ficha del empleado. Verifique los datos obligatorios.");
            }
        }

        // PUT: api/Empleados/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpleado(int id, Empleado empleado)
        {
            if (id != empleado.Id) return BadRequest("El ID no coincide.");
            if (IsGeneric) return Unauthorized();
            var empresaId = GetEmpresaId();
            var existente = await _context.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
            if (existente == null) return NotFound();
            empleado.Empresa = null!;
            empleado.EmpresaId = empresaId;
            _context.Entry(empleado).State = EntityState.Modified;
            _context.Entry(empleado).Property(x => x.FechaAlta).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmpleadoExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Empleados/5 (BORRADO LÓGICO)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmpleado(int id)
        {
            if (IsGeneric) return Unauthorized();
            var empresaId = GetEmpresaId();
            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
            if (empleado == null) return NotFound();
            empleado.FechaBaja = DateTime.Now;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool EmpleadoExists(int id)
        {
            var empresaId = GetEmpresaId();
            return _context.Empleados.Any(e => e.Id == id && e.EmpresaId == empresaId);
        }
    }
}