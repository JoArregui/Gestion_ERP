using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LlamadasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User);

        public LlamadasController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene llamadas solo de su empresa (genérico vacío)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LlamadaDTO>>> GetLlamadas()
        {
            if (IsGeneric) return Ok(new List<LlamadaDTO>());
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<LlamadaDTO>());
            var llamadas = await _context.Llamadas
                .Where(l => l.EmpresaId == empresaId)
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
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Unauthorized();
            if (string.IsNullOrWhiteSpace(dto.Empresa)) return BadRequest("Empresa es obligatoria");
            var entidad = new Llamada
            {
                EmpresaId = empresaId,
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