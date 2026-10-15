using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.Transversal;
using ERP.Services.Transversal;

namespace ERP.Api.Controllers.Transversal
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _report;

        public ReportController(ReportService report) => _report = report;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Definiciones ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool soloPublicos = false)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _report.ListarAsync(GetEmpresaId(), soloPublicos));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ReportDefinicion dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _report.CrearAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Ejecución + Export ───────────────────────────────────────
        [HttpPost("{id}/ejecutar")]
        public async Task<IActionResult> Ejecutar(int id, [FromBody] EjecutarReportDto dto)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var ejec = await _report.EjecutarAsync(GetEmpresaId(), id, dto.Parametros, User.Identity?.Name);
                return Ok(ejec);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("{id}/exportar")]
        public async Task<IActionResult> Exportar(int id, [FromQuery] string formato = "excel")
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            // TODO: Implementar exportación de última ejecución
            return BadRequest(new { Message = "No implementado aún" });
        }

        [HttpGet("historial")]
        public async Task<IActionResult> Historial([FromQuery] int? definicionId = null, [FromQuery] int take = 50)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _report.GetHistorialAsync(GetEmpresaId(), definicionId, take));
        }

        public sealed class EjecutarReportDto
        {
            public Dictionary<string, object>? Parametros { get; set; }
        }
    }
}