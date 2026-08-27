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
    public class LlamadasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LlamadasController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la lista de llamadas registradas en la base de datos
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LlamadaDTO>>> GetLlamadas()
        {
            var llamadas = await _context.Llamadas
                .OrderByDescending(l => l.Fecha)
                .Take(20)
                .ToListAsync();

            var resultado = llamadas.Select(l => new LlamadaDTO
            {
                Id = l.Id,
                Empresa = l.Empresa,
                Motivo = l.Motivo,
                Telefono = l.Telefono,
                Tiempo = l.Tiempo,
                Urgente = l.Urgente,
                Fecha = l.Fecha
            }).ToList();

            return Ok(resultado);
        }

        [HttpPost]
        public async Task<ActionResult<LlamadaDTO>> CrearLlamada([FromBody] LlamadaDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Empresa)) return BadRequest("Empresa es obligatoria");
            var entidad = new Llamada
            {
                Empresa = dto.Empresa,
                Motivo = dto.Motivo,
                Telefono = dto.Telefono ?? "",
                Tiempo = string.IsNullOrWhiteSpace(dto.Tiempo) ? "Ahora" : dto.Tiempo,
                Urgente = dto.Urgente,
                Fecha = DateTime.Now
            };
            _context.Llamadas.Add(entidad);
            await _context.SaveChangesAsync();
            dto.Id = entidad.Id;
            dto.Fecha = entidad.Fecha;
            return CreatedAtAction(nameof(GetLlamadas), new { id = entidad.Id }, dto);
        }
    }

    public class LlamadaDTO
    {
        public int Id { get; set; }
        public string Empresa { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Tiempo { get; set; } = string.Empty;
        public bool Urgente { get; set; }
        public DateTime Fecha { get; set; }
    }
}