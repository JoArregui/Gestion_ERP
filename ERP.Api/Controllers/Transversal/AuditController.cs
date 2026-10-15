using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.Transversal;
using ERP.Services.Transversal;

namespace ERP.Api.Controllers.Transversal
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly AuditService _audit;

        public AuditController(AuditService audit) => _audit = audit;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Configuración ─────────────────────────────────────────────
        [HttpGet("config")]
        public async Task<IActionResult> GetConfigs()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _audit.GetConfigsAsync(GetEmpresaId()));
        }

        [HttpPost("config")]
        public async Task<IActionResult> PostConfig([FromBody] AuditConfig dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _audit.UpsertConfigAsync(GetEmpresaId(), dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("config/{entidad}/toggle")]
        public async Task<IActionResult> ToggleConfig(string entidad, [FromQuery] bool activa)
        {
            var ok = await _audit.ToggleConfigAsync(GetEmpresaId(), entidad, activa);
            return ok ? Ok(new { Activa = activa }) : NotFound(new { Message = "Config no encontrada" });
        }

        // ── Logs ──────────────────────────────────────────────────────
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? entidad = null,
            [FromQuery] int? entidadId = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int take = 200)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _audit.GetLogsAsync(GetEmpresaId(), entidad, entidadId, desde, hasta, take));
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] DateTime? desde = null)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _audit.GetResumenAsync(GetEmpresaId(), desde));
        }

        // Helper: obtener logs de una entidad concreta
        [HttpGet("logs/{entidad}/{id}")]
        public async Task<IActionResult> GetLogsEntidad(string entidad, int id, [FromQuery] int take = 50)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _audit.GetLogsAsync(GetEmpresaId(), entidad, id, null, null, take));
        }
    }
}