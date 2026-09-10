using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmpleadosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmpleadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Empleados
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empleado>>> GetEmpleados()
        {
            // Cargamos los empleados incluyendo los datos de empresa si es necesario
            // El filtro de FechaBaja == null suele gestionarse globalmente o aquí
            return await _context.Empleados
                .Where(e => e.FechaBaja == null)
                .OrderBy(e => e.Apellidos)
                .ToListAsync();
        }

        // GET: api/Empleados/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Empleado>> GetEmpleado(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();
            return empleado;
        }

        // POST: api/Empleados
        [HttpPost]
        public async Task<ActionResult<Empleado>> PostEmpleado(Empleado empleado)
        {
            try 
            {
                // Limpieza de navegación: Evitamos que EF intente crear una empresa nueva
                empleado.Empresa = null!; 
                
                if (empleado.FechaAlta == default) empleado.FechaAlta = DateTime.Now;
                
                // Aseguramos que el estado inicial sea activo
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

            // Desvinculamos la entidad Empresa para que no de error al actualizar
            empleado.Empresa = null!;
            _context.Entry(empleado).State = EntityState.Modified;

            // Evitamos que se modifiquen campos sensibles por accidente en el PUT simple
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
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            // Marcamos la baja y guardamos
            empleado.FechaBaja = DateTime.Now;
            
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool EmpleadoExists(int id)
        {
            return _context.Empleados.Any(e => e.Id == id);
        }
    }
}