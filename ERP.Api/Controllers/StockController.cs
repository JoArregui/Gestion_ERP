using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.DTOs;
using ERP.Domain.Entities;
using ERP.Services;
using System.Security.Claims;

namespace ERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;

        public StockController(ApplicationDbContext context, StockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.local", System.StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.com", System.StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.local", System.StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.com", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Obtiene los indicadores clave de valoración de inventario — RGPD por empresa.
        /// </summary>
        [HttpGet("valoracion-dashboard")]
        public async Task<ActionResult<ValoracionStockDTO>> GetValoracionDashboard()
        {
            if (IsGeneric) return Ok(new ValoracionStockDTO { ValorTotalAlmacen = 0, TotalArticulosDiferentes = 0, CantidadTotalUnidades = 0, TopArticulosMasValiosos = new List<ArticuloValoradoDTO>() });
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new ValoracionStockDTO { ValorTotalAlmacen = 0, TotalArticulosDiferentes = 0, CantidadTotalUnidades = 0, TopArticulosMasValiosos = new List<ArticuloValoradoDTO>() });
            var articulos = await _context.Articulos
                .Where(a => a.EmpresaId == empresaId && a.Stock > 0)
                .ToListAsync();

            var dto = new ValoracionStockDTO
            {
                ValorTotalAlmacen = articulos.Sum(a => a.Stock * a.PrecioCompra),
                TotalArticulosDiferentes = articulos.Count,
                CantidadTotalUnidades = (double)articulos.Sum(a => a.Stock),
                TopArticulosMasValiosos = articulos
                    .OrderByDescending(a => a.Stock * a.PrecioCompra)
                    .Take(5)
                    .Select(a => new ArticuloValoradoDTO
                    {
                        Codigo = a.Codigo,
                        Descripcion = a.Descripcion,
                        Stock = (double)a.Stock,
                        PMP = a.PrecioCompra
                    })
                    .ToList()
            };

            return Ok(dto);
        }

        /// <summary>
        /// Obtiene el historial de movimientos (Kardex) — solo si el artículo es de su empresa.
        /// </summary>
        [HttpGet("movimientos/{articuloId}")]
        public async Task<ActionResult<IEnumerable<MovimientoStock>>> GetMovimientos(int articuloId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            if (IsGeneric) return Ok(new List<MovimientoStock>());
            var empresaId = GetEmpresaId();
            var articuloPertenece = await _context.Articulos.AnyAsync(a => a.Id == articuloId && a.EmpresaId == empresaId);
            if (!articuloPertenece) return Forbid();
            var query = _context.MovimientosStock
                .Where(m => m.ArticuloId == articuloId && m.EmpresaId == empresaId);

            if (desde.HasValue)
                query = query.Where(m => m.Fecha >= desde.Value);
            
            if (hasta.HasValue)
                query = query.Where(m => m.Fecha <= hasta.Value);

            var movimientos = await query
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.Id)
                .ToListAsync();

            if (movimientos == null)
                return NotFound($"No se han encontrado movimientos para el artículo con ID {articuloId}");

            return Ok(movimientos);
        }

        /// <summary>
        /// Realiza un ajuste de stock manual (inventario físico).
        /// Soporta entrada desde Web (ID) y Scanpal (Código de Barras).
        /// </summary>
        [HttpPost("ajuste-manual")]
        public async Task<IActionResult> AjusteManual([FromBody] AjusteStockDTO ajuste)
        {
            if (IsGeneric) return Unauthorized("Genérico sin empresa no opera stock.");
            var empresaIdChk = GetEmpresaId();
            if (empresaIdChk == 0) return Unauthorized("Sesión sin empresa.");
            if (ajuste.ArticuloId > 0)
            {
                var pertenece = await _context.Articulos.AnyAsync(a => a.Id == ajuste.ArticuloId && a.EmpresaId == empresaIdChk);
                if (!pertenece) return Forbid();
            }
            if (ajuste.ArticuloId >= 0)
            {
                try
                {
                    var nuevoStockSeguro = await _stockService.AjustarStockAsync(ajuste);
                    return Ok(new { mensaje = "Inventario regularizado correctamente.", nuevoStock = nuevoStockSeguro });
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            if (IsGeneric) return Unauthorized("Genérico sin empresa no opera stock.");
            var empresaIdAjuste = GetEmpresaId();
            if (empresaIdAjuste == 0) return Unauthorized("Sesión sin empresa.");
            Articulo? articulo;
            if (ajuste.ArticuloId > 0)
            {
                articulo = await _context.Articulos.FirstOrDefaultAsync(a => a.Id == ajuste.ArticuloId && a.EmpresaId == empresaIdAjuste);
            }
            else
            {
                articulo = await _context.Articulos.FirstOrDefaultAsync(a => a.Codigo == ajuste.CodigoBarras && a.EmpresaId == empresaIdAjuste);
            }
            if (articulo == null) return NotFound("Artículo no encontrado en su empresa");

            decimal stockObjetivo = ajuste.ArticuloId > 0 ? ajuste.NuevoStock : ajuste.CantidadReal;
            decimal diferencia = stockObjetivo - articulo.Stock;
            
            if (diferencia == 0) return Ok(new { mensaje = "El stock ya es correcto. No se requiere ajuste." });

            var movimiento = new MovimientoStock
            {
                ArticuloId = articulo.Id,
                EmpresaId = empresaIdAjuste,
                Fecha = DateTime.Now,
                TipoMovimiento = diferencia > 0 ? "ENTRADA" : "SALIDA",
                Cantidad = Math.Abs(diferencia),
                StockResultante = stockObjetivo,
                ReferenciaDocumento = "AJUSTE INV",
                Observaciones = ajuste.Motivo ?? $"Ajuste desde {ajuste.TerminalId}"
            };

            articulo.Stock = stockObjetivo;

            _context.MovimientosStock.Add(movimiento);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Inventario regularizado correctamente.", nuevoStock = articulo.Stock });
        }

        /// <summary>
        /// Reconstruye el inventario a una fecha — RGPD por empresa.
        /// </summary>
        [HttpGet("existencias-a-fecha")]
        public async Task<ActionResult<IEnumerable<object>>> GetStockAFecha([FromQuery] DateTime fechaCorte)
        {
            if (IsGeneric) return Ok(new List<object>());
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<object>());
            var articulos = await _context.Articulos.Where(a => a.EmpresaId == empresaId).ToListAsync();
            var movimientosPosteriores = await _context.MovimientosStock
                .Where(m => m.EmpresaId == empresaId && m.Fecha > fechaCorte)
                .ToListAsync();

            var informe = articulos.Select(a => {
                var movsArt = movimientosPosteriores.Where(m => m.ArticuloId == a.Id);
                
                decimal entradasPost = movsArt.Where(m => m.TipoMovimiento == "ENTRADA").Sum(m => m.Cantidad);
                decimal salidasPost = movsArt.Where(m => m.TipoMovimiento == "SALIDA").Sum(m => m.Cantidad);

                decimal stockCalculado = a.Stock - entradasPost + salidasPost;

                return new {
                    Id = a.Id,
                    Codigo = a.Codigo,
                    Descripcion = a.Descripcion,
                    StockActual = a.Stock,
                    StockAFecha = stockCalculado,
                    PMP = a.PrecioCompra,
                    PrecioCosto = a.PrecioCompra,
                    ValoracionAFecha = stockCalculado * a.PrecioCompra
                };
            }).ToList();

            return Ok(informe);
        }

        /// <summary>
        /// Obtiene datos para etiquetas — solo artículos de su empresa.
        /// </summary>
        [HttpPost("preparar-etiquetas")]
        public async Task<ActionResult<IEnumerable<EtiquetaArticuloDTO>>> PrepararEtiquetas([FromBody] List<int> articuloIds)
        {
            if (IsGeneric) return Ok(new List<EtiquetaArticuloDTO>());
            var empresaId = GetEmpresaId();
            var articulos = await _context.Articulos
                .Where(a => a.EmpresaId == empresaId && articuloIds.Contains(a.Id))
                .Select(a => new EtiquetaArticuloDTO
                {
                    Codigo = a.Codigo,
                    Descripcion = a.Descripcion,
                    Precio = a.PrecioVenta, // Asumimos que queremos imprimir el PVP en la etiqueta
                    CodigoBarras = a.Codigo, // Usamos el código como identificador de barras
                    CantidadAImprimir = 1
                })
                .ToListAsync();

            return Ok(articulos);
        }
    }
}
