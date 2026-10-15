using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.Transversal;
using ERP.Services.Transversal;

namespace ERP.Api.Controllers.Transversal
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase
    {
        private readonly WorkflowService _wf;

        public WorkflowController(WorkflowService wf) => _wf = wf;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Definiciones ─────────────────────────────────────────────
        [HttpGet("definiciones")]
        public async Task<IActionResult> GetDefiniciones()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _wf.ListarDefinicionesAsync(GetEmpresaId()));
        }

        [HttpPost("definiciones")]
        public async Task<IActionResult> PostDefinicion([FromBody] WorkflowDefinicion dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _wf.CrearDefinicionAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Ejecución manual / testing ───────────────────────────────
        [HttpPost("ejecutar")]
        public async Task<IActionResult> Ejecutar([FromBody] EjecutarWorkflowDto dto)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var res = await _wf.EvaluarYEjecutarAsync(GetEmpresaId(), dto.Entidad, dto.EntidadId, dto.Accion, null);
                return Ok(res);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Plantillas notificación ──────────────────────────────────
        [HttpGet("plantillas")]
        public async Task<IActionResult> GetPlantillas()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _wf.GetPlantillasAsync(GetEmpresaId()));
        }

        [HttpPost("plantillas")]
        public async Task<IActionResult> PostPlantilla([FromBody] NotificacionPlantilla dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _wf.UpsertPlantillaAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        public sealed class EjecutarWorkflowDto
        {
            public string Entidad { get; set; } = "";
            public int EntidadId { get; set; }
            public string Accion { get; set; } = "UPDATE";
        }
    }
}