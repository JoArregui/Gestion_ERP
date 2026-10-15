using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Data;
using ERP.Domain.Entities.TicketBai;
using ERP.Services.TicketBai;

namespace ERP.Api.Controllers.TicketBai
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TicketBaiController : ControllerBase
    {
        private readonly TicketBaiService _service;
        private readonly ApplicationDbContext _context;

        public TicketBaiController(TicketBaiService service, ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Configuración ─────────────────────────────────────────────
        [HttpGet("config")]
        public async Task<IActionResult> GetConfigs()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _service.GetConfigsAsync(GetEmpresaId()));
        }

        [HttpPost("config")]
        public async Task<IActionResult> PostConfig([FromBody] ConfiguracionTicketBai dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _service.UpsertConfigAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Registros ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? hacienda = null, [FromQuery] string? estado = null, [FromQuery] int take = 100)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _service.ListarAsync(GetEmpresaId(), hacienda, estado, take));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var r = await _service.ObtenerAsync(GetEmpresaId(), id);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPost("generar")]
        public async Task<IActionResult> Generar([FromBody] GenerarTicketBaiDto dto)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var doc = await _context.Documentos.FindAsync(dto.DocumentoId);
                if (doc == null || doc.EmpresaId != GetEmpresaId()) return NotFound(new { Message = "Documento no encontrado" });
                var registro = await _service.GenerarRegistroAsync(doc, dto.HaciendaForal ?? "BI");
                return Ok(registro);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/anular")]
        public async Task<IActionResult> Anular(int id)
        {
            try { return Ok(await _service.GenerarAnulacionAsync(id)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        public sealed class GenerarTicketBaiDto
        {
            public int DocumentoId { get; set; }
            public string? HaciendaForal { get; set; } = "BI";
        }
    }
}