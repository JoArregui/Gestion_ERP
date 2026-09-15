using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.DTOs;
using ERP.Domain.Entities;
using System.Globalization;
using ERP.Api.Services;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public DashboardController(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var eid) ? eid : 0;

        [HttpGet("resumen-financiero")]
        public async Task<ActionResult<DashboardDTO>> GetResumen()
        {
            // Genérico del primer onboarding nunca ve facturación (vacío)
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (ERP.Domain.Constants.BootstrapUser.IsBootstrap(email))
            {
                return Ok(new DashboardDTO
                {
                    TotalVentas = 0, TotalCompras = 0, TotalNominas = 0, BeneficioNeto = 0,
                    FacturasPendientesCobro = 0, ImportePendienteCobro = 0, FacturasVencidas = 0,
                    ArticulosStockBajo = 0, VentasMensuales = new List<GraficoVentasMes>()
                });
            }
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (!int.TryParse(empresaIdClaim, out var empresaId) || empresaId == 0)
            {
                return Ok(new DashboardDTO
                {
                    TotalVentas = 0, TotalCompras = 0, TotalNominas = 0, BeneficioNeto = 0,
                    FacturasPendientesCobro = 0, ImportePendienteCobro = 0, FacturasVencidas = 0,
                    ArticulosStockBajo = 0, VentasMensuales = new List<GraficoVentasMes>()
                });
            }
            var hoy = DateTime.Today;

            var ventasTotal = await _context.Documentos
                .Where(d => d.EmpresaId == empresaId && d.Tipo == TipoDocumento.Factura && !d.EsCompra)
                .Select(d => d.Total != 0
                    ? d.Total
                    : d.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario * (1 + l.PorcentajeIva / 100m)))
                .SumAsync();

            var comprasTotal = await _context.Documentos
                .Where(d => d.EmpresaId == empresaId && d.Tipo == TipoDocumento.Factura && d.EsCompra)
                .SumAsync(d => d.Total);

            var nominasTotal = await _context.Nominas
                .Where(n => n.Empleado != null && n.Empleado.EmpresaId == empresaId)
                .SumAsync(n => n.SalarioBase + n.Complementos);

            var pendientesQuery = _context.Vencimientos
                .AsNoTracking()
                .Where(v => v.EmpresaId == empresaId && v.Estado != "Pagado" && v.Documento != null && !v.Documento.EsCompra);

            // Agregados en BBDD: antes se materializaba toda la lista en memoria
            var pendientesCount = await pendientesQuery.CountAsync();
            var pendienteImporte = await pendientesQuery.SumAsync(v => (decimal?)v.Importe ?? 0);

            var vencidasCount = await pendientesQuery
                .CountAsync(v => v.FechaVencimiento < hoy);

            var stockCritico = await _context.Articulos
                .Where(a => a.EmpresaId == empresaId)
                .CountAsync(a => a.Stock < a.StockMinimo || a.Stock < 5);

            var seisMesesAtras = DateTime.Today.AddMonths(-5);
            var ventasPorMes = await _context.Documentos
                .Where(d => d.EmpresaId == empresaId && d.Tipo == TipoDocumento.Factura && !d.EsCompra && d.Fecha >= seisMesesAtras)
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
                FacturasPendientesCobro = pendientesCount,
                ImportePendienteCobro = pendienteImporte,
                FacturasVencidas = vencidasCount,
                ArticulosStockBajo = stockCritico,
                VentasMensuales = ventasPorMes
            });
        }

        [HttpGet("detalle/{tipo}")]
        public async Task<ActionResult<DashboardDetalleDTO>> GetDetalle(string tipo)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new DashboardDetalleDTO { Titulo = tipo == "stock-bajo" ? "ARTÍCULOS BAJO MÍNIMOS" : "VENCIMIENTOS IMPAGADOS", Items = new() });
            var detalle = new DashboardDetalleDTO
            {
                Titulo = tipo == "stock-bajo" ? "ARTÍCULOS BAJO MÍNIMOS" : "VENCIMIENTOS IMPAGADOS"
            };

            if (tipo == "stock-bajo")
            {
                detalle.Items = await _context.Articulos
                    .Where(a => a.EmpresaId == empresaId && (a.Stock < a.StockMinimo || a.Stock < 5))
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
                    .Where(v => v.EmpresaId == empresaId && v.Estado != "Pagado" && v.FechaVencimiento < DateTime.Today)
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
            catch
            {
                // Mensaje genérico: no se exponen detalles SMTP/infra al cliente.
                return StatusCode(500, "Error al enviar email.");
            }
        }
    }
}
