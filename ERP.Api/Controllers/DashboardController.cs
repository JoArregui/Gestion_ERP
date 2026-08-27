using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.DTOs;
using ERP.Domain.Entities;
using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using ERP.Api.Hubs;
using ERP.Api.Services;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IEmailService _emailService;

        public DashboardController(
            ApplicationDbContext context, 
            IHubContext<DashboardHub> hubContext,
            IEmailService emailService)
        {
            _context = context;
            _hubContext = hubContext;
            _emailService = emailService;
        }

        [HttpGet("resumen-financiero")]
        public async Task<ActionResult<DashboardDTO>> GetResumen()
        {
            var hoy = DateTime.Today;

            var ventasTotal = await _context.Documentos
                .Where(d => d.Tipo == TipoDocumento.Factura && !d.EsCompra)
                .Select(d => d.Total != 0
                    ? d.Total
                    : d.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario * (1 + l.PorcentajeIva / 100m)))
                .SumAsync();

            var comprasTotal = await _context.Documentos
                .Where(d => d.Tipo == TipoDocumento.Factura && d.EsCompra)
                .SumAsync(d => d.Total);

            var nominasTotal = await _context.Nominas
                .SumAsync(n => n.SalarioBase + n.Complementos);

            var pendientesQuery = _context.Vencimientos
                .Where(v => v.Estado != "Pagado" && v.Documento != null && !v.Documento.EsCompra);

            var pendientes = await pendientesQuery.ToListAsync();
            
            var vencidasCount = await pendientesQuery
                .CountAsync(v => v.FechaVencimiento < hoy);

            var stockCritico = await _context.Articulos
                .CountAsync(a => a.Stock < a.StockMinimo || a.Stock < 5);

            var seisMesesAtras = DateTime.Today.AddMonths(-5);
            var ventasPorMes = await _context.Documentos
                .Where(d => d.Tipo == TipoDocumento.Factura && !d.EsCompra && d.Fecha >= seisMesesAtras)
                .GroupBy(d => new { d.Fecha.Year, d.Fecha.Month })
                .Select(g => new GraficoVentasMes
                {
                    Mes = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yy", new CultureInfo("es-ES")),
                    Importe = g.Sum(d => d.Total != 0
                        ? d.Total
                        : d.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario * (1 + l.PorcentajeIva / 100m))),
                    Orden = g.Key.Year * 100 + g.Key.Month 
                })
                .OrderBy(x => x.Orden)
                .ToListAsync();

            return Ok(new DashboardDTO
            {
                TotalVentas = ventasTotal,
                TotalCompras = comprasTotal,
                TotalNominas = nominasTotal,
                BeneficioNeto = ventasTotal - comprasTotal - nominasTotal,
                FacturasPendientesCobro = pendientes.Count,
                ImportePendienteCobro = pendientes.Sum(p => p.Importe),
                FacturasVencidas = vencidasCount,
                ArticulosStockBajo = stockCritico,
                VentasMensuales = ventasPorMes
            });
        }

        [HttpGet("detalle/{tipo}")]
        public async Task<ActionResult<DashboardDetalleDTO>> GetDetalle(string tipo)
        {
            var detalle = new DashboardDetalleDTO 
            { 
                Titulo = tipo == "stock-bajo" ? "ARTÍCULOS BAJO MÍNIMOS" : "VENCIMIENTOS IMPAGADOS" 
            };

            if (tipo == "stock-bajo")
            {
                detalle.Items = await _context.Articulos
                    .Where(a => a.Stock < a.StockMinimo || a.Stock < 5)
                    .Select(a => new ItemDetalle {
                        IdRelacionado = a.Id,
                        Principal = a.Descripcion,
                        Secundario = a.Codigo,
                        Valor = $"{a.Stock:N2} uds.",
                        Estado = "Critico",
                        TipoEnlace = "articulo"
                    }).ToListAsync();
            }
            else if (tipo == "vencimientos")
            {
                detalle.Items = await _context.Vencimientos
                    .Include(v => v.Documento)
                    .ThenInclude(d => d!.Cliente)
                    .Where(v => v.Estado != "Pagado" && v.FechaVencimiento < DateTime.Today)
                    .Select(v => new ItemDetalle {
                        IdRelacionado = v.DocumentoId ?? 0,
                        Principal = v.Documento != null ? v.Documento.NumeroDocumento : (v.DocumentoId == null ? "NÓMINA" : "S/N"),
                        Secundario = (v.Documento != null && v.Documento.Cliente != null) ? v.Documento.Cliente.RazonSocial : (v.DocumentoId == null ? "Nómina interna" : "Sin Cliente"),
                        Valor = v.Importe.ToString("C2"),
                        Estado = "Vencido",
                        TipoEnlace = v.DocumentoId == null ? "nomina" : "factura"
                    }).ToListAsync();
            }

            return Ok(detalle);
        }

        [HttpPost("enviar-reporte-email")]
        public async Task<IActionResult> EnviarReporteEmail([FromBody] EnvioReporteDTO request)
        {
            if (string.IsNullOrEmpty(request.Destinatario)) 
                return BadRequest("El destinatario es obligatorio.");

            try
            {
                await _emailService.SendReporteAsync(request);
                return Ok(new { Message = "Reporte enviado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al enviar email: {ex.Message}");
            }
        }

        [HttpPost("notificar-cambio")]
        public async Task<IActionResult> NotificarCambio()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate");
            return Ok(new { Message = "Notificación enviada" });
        }
    }
}