using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities.Contabilidad;
using ERP.Services.Contabilidad;

namespace ERP.Api.Controllers.Contabilidad
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ActivosFijosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ActivoFijoService _service;

        public ActivosFijosController(ApplicationDbContext context, ActivoFijoService service)
        {
            _context = context;
            _service = service;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _service.ListarAsync(empresaId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var a = await _service.ObtenerAsync(GetEmpresaId(), id);
            return a == null ? NotFound() : Ok(a);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ActivoFijo dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var creado = await _service.CrearAsync(dto, User.Identity?.Name);
                return CreatedAtAction(nameof(Get), new { id = creado.Id }, creado);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/plan")]
        public async Task<IActionResult> RegenerarPlan(int id)
        {
            try
            {
                var plan = await _service.GenerarPlanAsync(GetEmpresaId(), id);
                return Ok(plan);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/contabilizar/{ejercicio}")]
        public async Task<IActionResult> Contabilizar(int id, int ejercicio)
        {
            try
            {
                var asiento = await _service.ContabilizarDotacionAsync(GetEmpresaId(), id, ejercicio, User.Identity?.Name);
                return Ok(new { asiento.Id, Referencia = asiento.ReferenciaCompleta, asiento.TotalDebe });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/baja")]
        public async Task<IActionResult> Baja(int id, [FromQuery] DateTime fecha, [FromQuery] string motivo = "Baja")
        {
            try
            {
                var a = await _service.DarDeBajaAsync(GetEmpresaId(), id, fecha == default ? DateTime.Today : fecha, motivo, User.Identity?.Name);
                return Ok(new { a.Estado, a.FechaBaja });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }
    }
}
