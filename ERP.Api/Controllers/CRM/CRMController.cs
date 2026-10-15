using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.CRM;
using ERP.Services.CRM;

namespace ERP.Api.Controllers.CRM
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CRMController : ControllerBase
    {
        private readonly CrmService _crm;

        public CRMController(CrmService crm) => _crm = crm;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Leads ──────────────────────────────────────────────
        [HttpGet("leads")]
        public async Task<IActionResult> GetLeads([FromQuery] EstadoLead? estado = null)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _crm.ListarLeadsAsync(GetEmpresaId(), estado));
        }

        [HttpPost("leads")]
        public async Task<IActionResult> PostLead([FromBody] Lead dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var creado = await _crm.CrearLeadAsync(dto, User.Identity?.Name);
                return CreatedAtAction(nameof(GetLeads), new { id = creado.Id }, creado);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("leads/{id}/estado")]
        public async Task<IActionResult> EstadoLead(int id, [FromQuery] EstadoLead estado)
        {
            try { return Ok(await _crm.CambiarEstadoLeadAsync(GetEmpresaId(), id, estado)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("leads/{id}/convertir")]
        public async Task<IActionResult> ConvertirLead(int id)
        {
            try
            {
                var (cliente, oportunidad) = await _crm.ConvertirLeadAsync(GetEmpresaId(), id, User.Identity?.Name);
                return Ok(new { ClienteId = cliente.Id, cliente.CodigoCliente, OportunidadId = oportunidad.Id });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Oportunidades / embudo ─────────────────────────────
        [HttpGet("oportunidades")]
        public async Task<IActionResult> GetOportunidades([FromQuery] EtapaOportunidad? etapa = null)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _crm.ListarOportunidadesAsync(GetEmpresaId(), etapa));
        }

        [HttpPost("oportunidades")]
        public async Task<IActionResult> PostOportunidad([FromBody] Oportunidad dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var creada = await _crm.CrearOportunidadAsync(dto, User.Identity?.Name);
                return CreatedAtAction(nameof(GetOportunidades), new { id = creada.Id }, creada);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("oportunidades/{id}/etapa")]
        public async Task<IActionResult> CambiarEtapa(int id, [FromQuery] EtapaOportunidad etapa, [FromQuery] string? motivo = null)
        {
            try { return Ok(await _crm.CambiarEtapaAsync(GetEmpresaId(), id, etapa, motivo)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("oportunidades/{id}/convertir-presupuesto")]
        public async Task<IActionResult> ConvertirPresupuesto(int id, [FromQuery] decimal? iva = null)
        {
            try
            {
                var doc = await _crm.ConvertirAPresupuestoAsync(GetEmpresaId(), id, iva);
                return Ok(new { doc.Id, doc.NumeroDocumento, doc.Total });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipeline()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _crm.GetPipelineAsync(GetEmpresaId()));
        }
    }
}
