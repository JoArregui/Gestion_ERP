using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Activado por defecto para seguridad multi-tenant
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper para obtener el EmpresaId del Token actual
        private int GetEmpresaId() => int.Parse(User.FindFirst("EmpresaId")?.Value ?? "0");

        // GET: api/clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            int empresaId = GetEmpresaId();
            
            // Filtramos por empresa. El Global Query Filter (si existe) 
            // se encargará de los inactivos, si no, lo añadimos aquí.
            return await _context.Clientes
                .Where(c => c.EmpresaId == empresaId)
                .OrderBy(c => c.RazonSocial)
                .ToListAsync();
        }

        // GET: api/clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            int empresaId = GetEmpresaId();
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

            if (cliente == null)
            {
                return NotFound(new { message = "Cliente no encontrado o sin permisos" });
            }

            return cliente;
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            cliente.EmpresaId = GetEmpresaId();
            
            if (cliente.FechaAlta == default)
                cliente.FechaAlta = DateTime.Now;

            // Validación de CIF usando el método de la Entidad
            if (!Cliente.ValidarCIF(cliente.CIF))
            {
                return BadRequest(new { message = "El formato del CIF/NIF es incorrecto" });
            }

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        // PUT: api/clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            if (id != cliente.Id) return BadRequest();

            int empresaId = GetEmpresaId();
            if (cliente.EmpresaId != empresaId) return Unauthorized();

            _context.Entry(cliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/clientes/5 (Borrado Lógico)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            int empresaId = GetEmpresaId();
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

            if (cliente == null) return NotFound();

            // Implementamos Borrado lógico
            cliente.IsActivo = false; 
            
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool ClienteExists(int id) => 
            _context.Clientes.Any(e => e.Id == id && e.EmpresaId == GetEmpresaId());
    }
}