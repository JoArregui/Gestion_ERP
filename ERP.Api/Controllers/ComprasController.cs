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
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (string.Equals(email, "admin@erp.local", System.StringComparison.OrdinalIgnoreCase) || string.Equals(email, "admin@erp.com", System.StringComparison.OrdinalIgnoreCase))
                return Ok(new List<DocumentoComercial>());
            if (!int.TryParse(User.FindFirst("EmpresaId")?.Value, out var empresaId) || empresaId == 0)
                return Ok(new List<DocumentoComercial>());
            return await _context.Documentos
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
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error al generar pedidos.", detalle = ex.Message });
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
    }
}