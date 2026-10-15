using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.Proyectos;
using ERP.Services.Proyectos;

namespace ERP.Api.Controllers.Proyectos
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProyectosController : ControllerBase
    {
        private readonly ProyectosService _proy;

        public ProyectosController(ProyectosService proy) => _proy = proy;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Proyectos ──────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] EstadoProyecto? estado = null)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _proy.ListarAsync(GetEmpresaId(), estado));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var p = await _proy.ObtenerAsync(GetEmpresaId(), id);
            return p == null ? NotFound() : Ok(p);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Proyecto dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _proy.CrearAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromQuery] EstadoProyecto estado)
        {
            try { return Ok(await _proy.CambiarEstadoAsync(GetEmpresaId(), id, estado)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("{id}/rentabilidad")]
        public async Task<IActionResult> Rentabilidad(int id)
        {
            try { return Ok(await _proy.GetRentabilidadAsync(GetEmpresaId(), id)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Hitos ──────────────────────────────────────────────────
        [HttpPost("{id}/hitos")]
        public async Task<IActionResult> PostHito(int id, [FromBody] HitoProyecto dto)
        {
            try { return Ok(await _proy.AddHitoAsync(GetEmpresaId(), id, dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Tareas / WBS ───────────────────────────────────────────
        [HttpGet("{id}/tareas")]
        public async Task<IActionResult> GetTareas(int id)
        {
            try { return Ok(await _proy.GetTareasAsync(GetEmpresaId(), id)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/tareas")]
        public async Task<IActionResult> PostTarea(int id, [FromBody] TareaProyecto dto)
        {
            try { return Ok(await _proy.AddTareaAsync(GetEmpresaId(), id, dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("tareas/{id}/estado")]
        public async Task<IActionResult> EstadoTarea(int id, [FromQuery] EstadoTarea estado, [FromQuery] decimal? porcentaje = null)
        {
            try { return Ok(await _proy.AvanzarTareaAsync(GetEmpresaId(), id, estado, porcentaje)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Timesheets / Imputación ────────────────────────────────
        [HttpPost("{id}/timesheets")]
        public async Task<IActionResult> PostTimesheet(int id, [FromBody] TimesheetProyecto dto)
        {
            dto.ProyectoId = id;
            try { return Ok(await _proy.ImputarHorasAsync(GetEmpresaId(), dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("{id}/timesheets")]
        public async Task<IActionResult> GetTimesheets(int id, [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            try { return Ok(await _proy.GetTimesheetsAsync(GetEmpresaId(), id, desde, hasta)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── SAT / Tickets ──────────────────────────────────────────
        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets([FromQuery] EstadoTicket? estado = null, [FromQuery] int? clienteId = null)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _proy.ListarTicketsAsync(GetEmpresaId(), estado, clienteId));
        }

        [HttpPost("tickets")]
        public async Task<IActionResult> PostTicket([FromBody] TicketSAT dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _proy.CrearTicketAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("tickets/{id}/estado")]
        public async Task<IActionResult> EstadoTicket(int id, [FromQuery] EstadoTicket estado, [FromQuery] string? tecnicoId = null, [FromQuery] string? solucion = null)
        {
            try { return Ok(await _proy.CambiarEstadoTicketAsync(GetEmpresaId(), id, estado, tecnicoId, solucion)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("tickets/{id}/comentarios")]
        public async Task<IActionResult> AddComentario(int id, [FromQuery] int empleadoId, [FromQuery] string comentario, [FromQuery] bool interno = true)
        {
            try { return Ok(await _proy.AddComentarioAsync(GetEmpresaId(), id, empleadoId, comentario, interno)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Contratos Mantenimiento ────────────────────────────────
        [HttpGet("contratos")]
        public async Task<IActionResult> GetContratos([FromQuery] bool soloActivos = true)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _proy.GetContratosAsync(GetEmpresaId(), soloActivos));
        }

        [HttpPost("contratos")]
        public async Task<IActionResult> PostContrato([FromBody] ContratoMantenimiento dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _proy.CrearContratoAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }
    }
}