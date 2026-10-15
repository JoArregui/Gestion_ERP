using Microsoft.AspNetCore.Mvc;
using ERP.Services;
using ERP.Domain.DTOs;
using ERP.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using ERP.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComprasController : ControllerBase
    {
        private readonly ComprasService _comprasService;
        private readonly ApplicationDbContext _context;

        public ComprasController(ComprasService comprasService, ApplicationDbContext context)
        {
            _comprasService = comprasService;
            _context = context;
        }

        [HttpPost("crear-pedido")]
        public async Task<IActionResult> CrearPedido([FromBody] PedidoCompraRequest request)
        {
            var empresaId = int.TryParse(User.FindFirst("EmpresaId")?.Value, out var claimEmpresaId)
                ? claimEmpresaId
                : 0;
            if (empresaId <= 0) return Unauthorized("Sesión sin empresa asociada.");

            try
            {
                var pedido = await _comprasService.CrearPedidoAsync(request, empresaId);
                return Ok(new { id = pedido.Id, numeroDocumento = pedido.NumeroDocumento });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene la lista de pedidos de compra que aún no han sido recepcionados (contabilizados).
        /// Incluye la información del proveedor, las líneas y los datos del artículo.
        /// </summary>
        [HttpGet("pendientes")]
        public async Task<ActionResult<IEnumerable<DocumentoComercial>>> GetPedidosPendientes()
        {
            var empresaId = int.TryParse(User.FindFirst("EmpresaId")?.Value, out var eid) ? eid : 0;
            if (empresaId == 0) return Ok(new List<DocumentoComercial>());
            return await _context.Documentos
                .AsNoTracking()
                .Include(d => d.Proveedor)
                .Include(d => d.Lineas)
                    .ThenInclude(l => l.Articulo)
                .Where(d => d.EmpresaId == empresaId && d.EsCompra && d.Tipo == TipoDocumento.Pedido && !d.IsContabilizado)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();
        }

        /// <summary>
        /// Endpoint para generar pedidos automáticos basados en una lista de alertas de stock.
        /// </summary>
        [HttpPost("generar-desde-alertas")]
        public async Task<IActionResult> GenerarDesdeAlertas([FromBody] List<AlertaStockDTO> alertas)
        {
            try
            {
                int generados = await _comprasService.GenerarPedidoDesdeAlertas(alertas);
                return Ok(new { mensaje = $"Se han generado {generados} pedidos correctamente." });
            }
            catch
            {
                return BadRequest(new { mensaje = "Error al generar pedidos." });
            }
        }

        /// <summary>
        /// Confirma la recepción de un pedido, lo convierte en factura y actualiza el stock.
        /// </summary>
        [HttpPost("recepcionar/{id}")]
        public async Task<IActionResult> Recepcionar(int id, [FromQuery] string numeroAlbaran)
        {
            if (string.IsNullOrEmpty(numeroAlbaran))
                return BadRequest("El número de albarán o factura del proveedor es obligatorio.");

            try 
            {
                var (resultado, mensajeError) = await _comprasService.RecepcionarPedido(id, numeroAlbaran);

                if (resultado)
                    return Ok(new { mensaje = "Pedido recepcionado, stock actualizado y precio medio recalculado." });

                return BadRequest(new { mensaje = mensajeError ?? "Error desconocido al procesar la recepción." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error interno al procesar la recepción.", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Recepción con 3-way matching pedido → albarán → factura (Fase C3).
        /// </summary>
        [HttpPost("recepcionar-con-factura/{id}")]
        public async Task<IActionResult> RecepcionarConFactura(int id, [FromQuery] string numeroAlbaran, [FromQuery] string numeroFactura, [FromQuery] decimal totalFactura, [FromQuery] decimal toleranciaPct = 2)
        {
            if (string.IsNullOrEmpty(numeroAlbaran)) return BadRequest("El número de albarán es obligatorio.");
            var (ok, err) = await _comprasService.RecepcionarConFacturaAsync(id, numeroAlbaran, numeroFactura, totalFactura, toleranciaPct);
            return ok ? Ok(new { mensaje = "Recepción 3-way validada, stock actualizado." }) : BadRequest(new { mensaje = err });
        }

        // ── Homologación (Fase C2) ─────────────────────────────
        [HttpPost("proveedores/{id}/homologar")]
        public async Task<IActionResult> Homologar(int id, [FromQuery] string? certificado = null)
        {
            var empresaId = EmpresaId();
            if (empresaId == 0) return Unauthorized("Sesión sin empresa asociada.");
            try { return Ok(await _comprasService.HomologarProveedorAsync(empresaId, id, certificado, User.Identity?.Name)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("proveedores/{id}/bloquear")]
        public async Task<IActionResult> Bloquear(int id, [FromQuery] string motivo = "")
        {
            var empresaId = EmpresaId();
            if (empresaId == 0) return Unauthorized("Sesión sin empresa asociada.");
            try { return Ok(await _comprasService.BloquearProveedorAsync(empresaId, id, motivo)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("proveedores/{id}/evaluar")]
        public async Task<IActionResult> Evaluar(int id, [FromQuery] int? calidad = null, [FromQuery] int? plazo = null)
        {
            var empresaId = EmpresaId();
            if (empresaId == 0) return Unauthorized("Sesión sin empresa asociada.");
            try { return Ok(await _comprasService.EvaluarProveedorAsync(empresaId, id, calidad, plazo)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        // ── RFQ (Fase C1) ────────────────────────────────────
        [HttpGet("rfq")]
        public async Task<IActionResult> GetRfqs()
        {
            var empresaId = EmpresaId();
            if (empresaId == 0) return Ok(new List<object>());
            return Ok(await _context.SolicitudesOferta
                .Include(s => s.Lineas).Include(s => s.Ofertas)
                .Where(s => s.EmpresaId == empresaId).OrderByDescending(s => s.FechaCreacion).ToListAsync());
        }

        [HttpPost("rfq")]
        public async Task<IActionResult> PostRfq([FromBody] CrearRfqDto dto)
        {
            var empresaId = EmpresaId();
            if (empresaId == 0) return Unauthorized("Sesión sin empresa asociada.");
            try
            {
                var rfq = await _comprasService.CrearRFQAsync(empresaId,
                    dto.Lineas.Select(l => (l.ArticuloId, l.Cantidad)).ToList(),
                    dto.FechaLimite, dto.Notas, User.Identity?.Name);
                return CreatedAtAction(nameof(GetRfqs), new { id = rfq.Id }, rfq);
            }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("rfq/{id}/enviar")]
        public async Task<IActionResult> EnviarRfq(int id, [FromBody] List<int> proveedorIds)
        {
            try { return Ok(await _comprasService.EnviarRFQAsync(EmpresaId(), id, proveedorIds)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("rfq/{id}/ofertas")]
        public async Task<IActionResult> PostOferta(int id, [FromBody] RegistrarOfertaDto dto)
        {
            try
            {
                var oferta = await _comprasService.RegistrarOfertaAsync(EmpresaId(), id, dto.ProveedorId,
                    dto.Lineas.Select(l => (l.ArticuloId, l.Cantidad, l.Precio)).ToList(), dto.PlazoDias, dto.Notas);
                return CreatedAtAction(nameof(GetComparativa), new { id }, oferta);
            }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpGet("rfq/{id}/comparativa")]
        public async Task<IActionResult> GetComparativa(int id)
        {
            try { return Ok(await _comprasService.GetComparativaAsync(EmpresaId(), id)); }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        [HttpPost("rfq/{id}/adjudicar")]
        public async Task<IActionResult> Adjudicar(int id, [FromQuery] int ofertaId)
        {
            try
            {
                var pedido = await _comprasService.AdjudicarAsync(EmpresaId(), id, ofertaId);
                return Ok(new { pedido.Id, pedido.NumeroDocumento, pedido.Total });
            }
            catch (Exception ex) { return BadRequest(new { mensaje = ex.Message }); }
        }

        private int EmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var eid) ? eid : 0;

        public sealed class CrearRfqDto
        {
            public List<LineaRfqDto> Lineas { get; set; } = new();
            public DateTime? FechaLimite { get; set; }
            public string? Notas { get; set; }
        }
        public sealed class LineaRfqDto
        {
            public int ArticuloId { get; set; }
            public decimal Cantidad { get; set; }
        }
        public sealed class RegistrarOfertaDto
        {
            public int ProveedorId { get; set; }
            public List<LineaOfertaDto> Lineas { get; set; } = new();
            public int? PlazoDias { get; set; }
            public string? Notas { get; set; }
        }
        public sealed class LineaOfertaDto
        {
            public int ArticuloId { get; set; }
            public decimal Cantidad { get; set; }
            public decimal Precio { get; set; }
        }
    }
}