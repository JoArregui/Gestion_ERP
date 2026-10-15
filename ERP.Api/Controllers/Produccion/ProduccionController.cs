using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.Produccion;
using ERP.Services.Produccion;

namespace ERP.Api.Controllers.Produccion
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProduccionController : ControllerBase
    {
        private readonly ProduccionService _prod;

        public ProduccionController(ProduccionService prod) => _prod = prod;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── BOM ──────────────────────────────────────────────────────
        [HttpGet("bom/{articuloPadreId}")]
        public async Task<IActionResult> GetBOM(int articuloPadreId)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _prod.GetBOMAsync(GetEmpresaId(), articuloPadreId));
        }

        [HttpPost("bom")]
        public async Task<IActionResult> PostBOM([FromBody] BOM dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _prod.AddComponenteAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Centros de Trabajo ──────────────────────────────────────
        [HttpGet("centros")]
        public async Task<IActionResult> GetCentros()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _prod.GetCentrosAsync(GetEmpresaId()));
        }

        [HttpPost("centros")]
        public async Task<IActionResult> PostCentro([FromBody] CentroTrabajo dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _prod.CrearCentroAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Hoja de Ruta ────────────────────────────────────────────
        [HttpGet("hojaruta/{articuloId}")]
        public async Task<IActionResult> GetHojaRuta(int articuloId)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _prod.GetHojaRutaAsync(GetEmpresaId(), articuloId));
        }

        [HttpPost("hojaruta")]
        public async Task<IActionResult> PostOperacion([FromBody] HojaRuta dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _prod.AddOperacionAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Órdenes de Fabricación ──────────────────────────────────
        [HttpGet("of")]
        public async Task<IActionResult> GetOFs([FromQuery] EstadoOF? estado = null)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _prod.ListarOFAsync(GetEmpresaId(), estado));
        }

        [HttpGet("of/{id}")]
        public async Task<IActionResult> GetOF(int id)
        {
            var of = await _prod.GetOFAsync(GetEmpresaId(), id);
            return of == null ? NotFound() : Ok(of);
        }

        [HttpPost("of")]
        public async Task<IActionResult> PostOF([FromBody] OrdenFabricacion dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _prod.CrearOFAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("of/{id}/iniciar")]
        public async Task<IActionResult> IniciarOF(int id)
        {
            try { return Ok(await _prod.IniciarOFAsync(GetEmpresaId(), id)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("of/{id}/imputar")]
        public async Task<IActionResult> Imputar(int id, [FromQuery] int operacionId, [FromQuery] decimal tiempoPrepMin, [FromQuery] decimal tiempoUnidadMin, [FromQuery] decimal unidades, [FromQuery] string? operarioId = null)
        {
            try { return Ok(await _prod.ImputarHorasAsync(GetEmpresaId(), id, operacionId, tiempoPrepMin, tiempoUnidadMin, unidades, operarioId)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("of/{id}/terminar")]
        public async Task<IActionResult> TerminarOF(int id, [FromQuery] decimal cantidadBuena, [FromQuery] decimal cantidadRechazada = 0)
        {
            try { return Ok(await _prod.TerminarOFAsync(GetEmpresaId(), id, cantidadBuena, cantidadRechazada)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("of/{id}/coste-unitario")]
        public async Task<IActionResult> CosteUnitario(int id)
        {
            try { return Ok(new { CosteUnitario = await _prod.GetCosteUnitarioAsync(GetEmpresaId(), id) }); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }
    }
}