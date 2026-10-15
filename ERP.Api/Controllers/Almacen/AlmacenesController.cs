using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.Almacen;
using EntAlm = ERP.Domain.Entities.Almacen.Almacen;
using ERP.Services.Almacen;

namespace ERP.Api.Controllers.Almacen
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AlmacenesController : ControllerBase
    {
        private readonly AlmacenService _almacen;

        public AlmacenesController(AlmacenService almacen) => _almacen = almacen;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Almacenes ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAlmacenes()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _almacen.ListarAlmacenesAsync(GetEmpresaId()));
        }

        [HttpPost]
        public async Task<IActionResult> PostAlmacen([FromBody] EntAlm dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var creado = await _almacen.CrearAlmacenAsync(dto);
                return CreatedAtAction(nameof(GetAlmacenes), new { id = creado.Id }, creado);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Ubicaciones ────────────────────────────────────────
        [HttpGet("{almacenId}/ubicaciones")]
        public async Task<IActionResult> GetUbicaciones(int almacenId)
        {
            try { return Ok(await _almacen.ListarUbicacionesAsync(GetEmpresaId(), almacenId)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{almacenId}/ubicaciones")]
        public async Task<IActionResult> PostUbicacion(int almacenId, [FromBody] Ubicacion dto)
        {
            dto.AlmacenId = almacenId;
            try
            {
                var creada = await _almacen.CrearUbicacionAsync(GetEmpresaId(), dto);
                return CreatedAtAction(nameof(GetUbicaciones), new { almacenId }, creada);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Movimientos ────────────────────────────────────────
        [HttpPost("ubicar")]
        public async Task<IActionResult> Ubicar([FromQuery] int articuloId, [FromQuery] int ubicacionId, [FromQuery] decimal cantidad)
        {
            try { return Ok(await _almacen.UbicarStockAsync(GetEmpresaId(), articuloId, ubicacionId, cantidad)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("mover")]
        public async Task<IActionResult> Mover([FromQuery] int articuloId, [FromQuery] int origenId, [FromQuery] int? destinoId, [FromQuery] decimal cantidad)
        {
            try
            {
                await _almacen.MoverStockAsync(GetEmpresaId(), articuloId, origenId, destinoId, cantidad);
                return Ok(new { Message = "Movimiento registrado" });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("detalle/{articuloId}")]
        public async Task<IActionResult> Detalle(int articuloId)
        {
            try { return Ok(await _almacen.GetDetalleAsync(GetEmpresaId(), articuloId)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // ── Picking / reposición (D2) ──────────────────────────
        [HttpGet("picking/{documentoId}")]
        public async Task<IActionResult> Picking(int documentoId)
        {
            try { return Ok(await _almacen.GenerarPickingAsync(GetEmpresaId(), documentoId)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("reposicion")]
        public async Task<IActionResult> Reposicion()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _almacen.GetReposicionAsync(GetEmpresaId()));
        }
    }
}
