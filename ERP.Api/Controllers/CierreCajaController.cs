using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities;
using ERP.Data;
using ERP.Services;
using System.Text.Json;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CierreCajaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PdfService _pdfService;

        public CierreCajaController(ApplicationDbContext context, PdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        private int GetEmpresaId()
        {
            var claim = User.FindFirst("EmpresaId")?.Value;
            return int.TryParse(claim, out var empresaId) && empresaId > 0
                ? empresaId
                : 0;
        }

        private bool PerteneceAEmpresa(int empresaId) => empresaId == GetEmpresaId();

        [HttpGet("totales-pendientes/{empresaId}")]
        public async Task<ActionResult<CierreCaja>> GetTotalesPendientes(int empresaId)
        {
            if (!PerteneceAEmpresa(empresaId)) return Forbid();

            try
            {
                var docs = await _context.Documentos
                    .Include(d => d.Lineas)
                    .Where(d => d.EmpresaId == empresaId && !d.IsContabilizado && !d.EsCompra && d.Tipo == TipoDocumento.Factura)
                    .ToListAsync();

                if (!docs.Any()) return Ok(new CierreCaja { EmpresaId = empresaId });

                var lineas = docs.SelectMany(d => d.Lineas).ToList();

                // Agrupación por Categorías (usando la nueva propiedad en DocumentoLinea)
                var ventasPorCategoria = lineas
                    .GroupBy(l => l.CategoriaNombre ?? "General")
                    .Select(g => new { Categoria = g.Key, Total = g.Sum(x => x.Cantidad * x.PrecioUnitario) })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                // Agrupación por Usuarios (usando la nueva propiedad en DocumentoComercial)
                var ventasPorUsuario = docs
                    .GroupBy(d => d.UsuarioNombre ?? "TPV Principal")
                    .Select(g => new { Usuario = g.Key, Total = g.Sum(x => x.Total) })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                var cierre = new CierreCaja
                {
                    EmpresaId = empresaId,
                    FechaCierre = DateTime.Now,
                    Terminal = "TPV-01",
                    TotalVentasEfectivo = docs.Where(d => d.MetodoPago == "Efectivo").Sum(d => d.Total),
                    TotalVentasTarjeta = docs.Where(d => d.MetodoPago == "Tarjeta").Sum(d => d.Total),

                    Base21 = lineas.Where(l => l.PorcentajeIva == 21).Sum(l => l.Cantidad * l.PrecioUnitario),
                    Iva21 = lineas.Where(l => l.PorcentajeIva == 21).Sum(l => (l.Cantidad * l.PrecioUnitario) * 0.21m),
                    Base10 = lineas.Where(l => l.PorcentajeIva == 10).Sum(l => l.Cantidad * l.PrecioUnitario),
                    Iva10 = lineas.Where(l => l.PorcentajeIva == 10).Sum(l => (l.Cantidad * l.PrecioUnitario) * 0.10m),
                    Base4 = lineas.Where(l => l.PorcentajeIva == 4).Sum(l => l.Cantidad * l.PrecioUnitario),
                    Iva4 = lineas.Where(l => l.PorcentajeIva == 4).Sum(l => (l.Cantidad * l.PrecioUnitario) * 0.04m),

                    TotalIva = docs.Sum(d => d.TotalIva),
                    DataCategoriasJson = JsonSerializer.Serialize(ventasPorCategoria),
                    DataUsuariosJson = JsonSerializer.Serialize(ventasPorUsuario),
                    Observaciones = ""
                };

                return Ok(cierre);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener datos: {ex.Message}");
            }
        }

        [HttpPost("ejecutar")]
        public async Task<IActionResult> EjecutarCierre([FromBody] CierreCaja cierre)
        {
            if (cierre == null) return BadRequest("Datos de cierre inválidos.");
            if (!PerteneceAEmpresa(cierre.EmpresaId)) return Forbid();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                cierre.IsProcesado = true;
                cierre.FechaCierre = DateTime.Now;
                cierre.Empresa = null; // Evitar conflictos de tracking

                _context.CierresCaja.Add(cierre);

                var documentos = await _context.Documentos
                    .Where(d => d.EmpresaId == cierre.EmpresaId && !d.IsContabilizado && !d.EsCompra && d.Tipo == TipoDocumento.Factura)
                    .ToListAsync();

                foreach (var doc in documentos)
                {
                    doc.IsContabilizado = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { id = cierre.Id, message = "Cierre de caja completado con éxito" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error crítico: {ex.Message}");
            }
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DescargarCierrePdf(int id)
        {
            var empresaId = GetEmpresaId();
            var cierre = await _context.CierresCaja
                .Include(c => c.Empresa)
                .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

            if (cierre == null || cierre.Empresa == null) return NotFound();

            var pdfBytes = _pdfService.GenerarCierreCajaPdf(cierre, cierre.Empresa);
            return File(pdfBytes, "application/pdf", $"Cierre_{cierre.FechaCierre:yyyyMMdd}_{id}.pdf");
        }

        [HttpGet("historial/{empresaId}")]
        public async Task<ActionResult<List<CierreCaja>>> GetHistorial(int empresaId)
        {
            if (!PerteneceAEmpresa(empresaId)) return Forbid();

            return await _context.CierresCaja
                .Where(c => c.EmpresaId == empresaId)
                .OrderByDescending(c => c.FechaCierre)
                .Take(50) // Limitamos a los últimos 50 por rendimiento
                .ToListAsync();
        }
    }
}