using Microsoft.AspNetCore.Mvc;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TareasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TareasController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la lista de tareas registradas en el sistema
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TareaDTO>>> GetTareas()
        {
            var tareas = await _context.Tareas
                .OrderBy(t => t.Hora)
                .ToListAsync();

            var resultado = tareas.Select(c => new TareaDTO
            {
                Id = c.Id,
                Hora = c.Hora,
                Titulo = c.Titulo,
                Descripcion = c.Descripcion ?? string.Empty,
                Prioridad = c.Prioridad,
                Completada = c.Completada
            }).ToList();

            return Ok(resultado);
        }

        [HttpPost]
        public async Task<ActionResult<TareaDTO>> CrearTarea([FromBody] TareaDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("El título es obligatorio");
            var entidad = new Tarea
            {
                Hora = string.IsNullOrWhiteSpace(dto.Hora) ? DateTime.Now.ToString("HH:mm") : dto.Hora,
                Titulo = dto.Titulo,
                Descripcion = dto.Descripcion,
                Prioridad = string.IsNullOrWhiteSpace(dto.Prioridad) ? "MEDIA" : dto.Prioridad.ToUpper(),
                Completada = dto.Completada
            };
            _context.Tareas.Add(entidad);
            await _context.SaveChangesAsync();
            dto.Id = entidad.Id;
            return CreatedAtAction(nameof(GetTareas), new { id = entidad.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarTarea(int id, [FromBody] TareaDTO dto)
        {
            var entidad = await _context.Tareas.FindAsync(id);
            if (entidad == null) return NotFound();
            entidad.Hora = dto.Hora;
            entidad.Titulo = dto.Titulo;
            entidad.Descripcion = dto.Descripcion;
            entidad.Prioridad = dto.Prioridad;
            entidad.Completada = dto.Completada;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarTarea(int id)
        {
            var entidad = await _context.Tareas.FindAsync(id);
            if (entidad == null) return NotFound();
            _context.Tareas.Remove(entidad);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleCompletada(int id)
        {
            var entidad = await _context.Tareas.FindAsync(id);
            if (entidad == null) return NotFound();
            entidad.Completada = !entidad.Completada;
            await _context.SaveChangesAsync();
            return Ok(new { entidad.Id, entidad.Completada });
        }
    }

    public class TareaDTO
    {
        public int Id { get; set; }
        public string Hora { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public bool Completada { get; set; }
    }
}