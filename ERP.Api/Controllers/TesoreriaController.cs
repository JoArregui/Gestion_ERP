using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Bancario;
using ERP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TesoreriaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PdfService _pdfService;

        public TesoreriaController(ApplicationDbContext context, PdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Método privado para garantizar el aislamiento Multi-tenant
        private int GetEmpresaId()
        {
            var claim = User.FindFirst("EmpresaId")?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }

        [HttpGet("pendientes-cobro")]
        public async Task<IActionResult> GetPendientesCobro()
        {
            int empresaId = GetEmpresaId();
            
            // Obtenemos vencimientos de ventas (cobros) pendientes
            var cobros = await _context.Vencimientos
                .Include(v => v.Documento)
                .ThenInclude(d => d!.Cliente)
                .Where(v => v.EmpresaId == empresaId && 
                            v.Estado == "Pendiente" && 
                            v.Documento != null && 
                            !v.Documento.EsCompra)
                .OrderBy(v => v.FechaVencimiento)
                .ToListAsync();

            return Ok(cobros);
        }

        [HttpGet("pendientes-pago")]
        public async Task<IActionResult> GetPendientesPago()
        {
            int empresaId = GetEmpresaId();

            // Obtenemos obligaciones: Facturas de proveedores y Nóminas
            var pagos = await _context.Vencimientos
                .Include(v => v.Documento)
                .ThenInclude(d => d!.Proveedor)
                .Where(v => v.EmpresaId == empresaId && 
                            v.Estado == "Pendiente" && 
                            (v.Documento == null || v.Documento.EsCompra))
                .OrderBy(v => v.FechaVencimiento)
                .ToListAsync();

            return Ok(pagos);
        }

        [HttpGet("vencimiento/{id}/pdf")]
        public async Task<IActionResult> PdfVencimiento(int id)
        {
            var venc = await _context.Vencimientos.FirstOrDefaultAsync(v => v.Id == id && v.EmpresaId == GetEmpresaId());
            if (venc == null) return NotFound("Vencimiento no encontrado");
            // Si es de documento comercial -> PDF factura/albarán
            if (venc.DocumentoId.HasValue)
            {
                var doc = await _context.Documentos.Include(d => d.Lineas).Include(d => d.Cliente).FirstOrDefaultAsync(d => d.Id == venc.DocumentoId.Value);
                if (doc == null) return NotFound("Documento no encontrado");
                var emp = await _context.Empresas.FindAsync(doc.EmpresaId);
                var pdf = _pdfService.GenerarFacturaPdf(doc, emp!, doc.Cliente);
                return File(pdf, "application/pdf", $"DOC_{doc.NumeroDocumento}.pdf");
            }
            else
            {
                // Nómina: buscar nómina por empresa/mes/año/importe
                var nomina = await _context.Nominas.Include(n => n.Empleado).FirstOrDefaultAsync(n => n.Empleado != null && n.Empleado.EmpresaId == venc.EmpresaId && n.Mes == venc.FechaVencimiento.Month && n.Anio == venc.FechaVencimiento.Year && (n.SalarioBase + n.Complementos - n.Deducciones) == venc.Importe);
                // Fallback: última nómina de esa empresa/mes
                nomina ??= await _context.Nominas.Include(n => n.Empleado).Where(n => n.Empleado != null && n.Empleado.EmpresaId == venc.EmpresaId && n.Mes == venc.FechaVencimiento.Month && n.Anio == venc.FechaVencimiento.Year).OrderByDescending(n => n.Id).FirstOrDefaultAsync();
                if (nomina == null || nomina.Empleado == null) return NotFound("Nómina no encontrada para este vencimiento");
                var emp = await _context.Empresas.FindAsync(nomina.Empleado.EmpresaId);
                var pdf = _pdfService.GenerarNominaPdf(nomina, nomina.Empleado, emp!);
                return File(pdf, "application/pdf", $"NOMINA_{nomina.Anio}_{nomina.Mes:D2}.pdf");
            }
        }

        [HttpPost("liquidar/{id}")]
        public async Task<IActionResult> LiquidarVencimiento(int id, [FromQuery] string metodoPago)
        {
            var vencimiento = await _context.Vencimientos
                .FirstOrDefaultAsync(v => v.Id == id && v.EmpresaId == GetEmpresaId());

            if (vencimiento == null)
                return NotFound("Registro de tesorería no encontrado o sin permisos.");

            if (vencimiento.Estado == "Pagado")
                return BadRequest("Este vencimiento ya fue liquidado anteriormente.");
            if (vencimiento.Estado == "Anulado")
                return BadRequest("No se puede liquidar un vencimiento anulado.");

            vencimiento.Estado = "Pagado";
            vencimiento.FechaPago = DateTime.Now;
            vencimiento.MetodoPago = string.IsNullOrEmpty(metodoPago) ? "Efectivo/Caja" : metodoPago;

            await _context.SaveChangesAsync();

            return Ok(new { 
                Status = "Success", 
                Message = $"Vencimiento {id} marcado como pagado el {vencimiento.FechaPago}." 
            });
        }

        [HttpPut("vencimiento/{id}")]
        public async Task<IActionResult> ModificarVencimiento(int id, [FromBody] Vencimiento dto)
        {
            var venc = await _context.Vencimientos.FirstOrDefaultAsync(v => v.Id == id && v.EmpresaId == GetEmpresaId());
            if (venc == null) return NotFound("Vencimiento no encontrado");
            if (venc.Estado == "Pagado") return BadRequest(new { Message = "No se puede modificar un vencimiento ya pagado. Anule y cree uno nuevo." });
            if (venc.Estado == "Anulado") return BadRequest(new { Message = "Vencimiento anulado no editable." });
            // Solo se permite modificar importe, fecha y método si es pendiente
            if (dto.Importe <= 0) return BadRequest(new { Message = "Importe debe ser positivo" });
            venc.Importe = dto.Importe;
            venc.FechaVencimiento = dto.FechaVencimiento;
            if (!string.IsNullOrWhiteSpace(dto.MetodoPago)) venc.MetodoPago = dto.MetodoPago;
            await _context.SaveChangesAsync();
            return Ok(venc);
        }

        [HttpPost("anular/{id}")]
        public async Task<IActionResult> AnularVencimiento(int id)
        {
            var venc = await _context.Vencimientos.FirstOrDefaultAsync(v => v.Id == id && v.EmpresaId == GetEmpresaId());
            if (venc == null) return NotFound("Vencimiento no encontrado");
            if (venc.Estado == "Pagado") return BadRequest(new { Message = "No se puede anular un vencimiento pagado. Genere rectificativa." });
            if (venc.Estado == "Anulado") return BadRequest(new { Message = "Ya está anulado" });
            venc.Estado = "Anulado";
            venc.FechaPago = null;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Vencimiento anulado correctamente" });
        }

        // Fase A3: previsión cash-flow desde vencimientos pendientes + remesas validadas no enviadas.
        [HttpGet("prevision")]
        public async Task<IActionResult> GetPrevision([FromQuery] int dias = 90)
        {
            int empresaId = GetEmpresaId();
            if (empresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            if (dias <= 0 || dias > 365) dias = 90;
            var hoy = DateTime.Today;
            var hasta = hoy.AddDays(dias);

            var vencs = await _context.Vencimientos
                .Include(v => v.Documento)
                .Where(v => v.EmpresaId == empresaId && v.Estado == "Pendiente"
                    && v.FechaVencimiento <= hasta)
                .ToListAsync();

            var remesasPend = await _context.RemesasSEPA
                .Where(r => r.EmpresaId == empresaId
                    && (r.Estado == EstadoRemesaSEPA.Borrador || r.Estado == EstadoRemesaSEPA.Validada)
                    && r.FechaEjecucion <= hasta)
                .Select(r => new { r.FechaEjecucion, r.ImporteTotal, r.Tipo })
                .ToListAsync();

            decimal vencidoCobro = vencs.Where(v => v.Documento != null && !v.Documento.EsCompra && v.FechaVencimiento < hoy).Sum(v => v.Importe);
            decimal vencidoPago = vencs.Where(v => (v.Documento == null || v.Documento.EsCompra) && v.FechaVencimiento < hoy).Sum(v => v.Importe);
            decimal totalCobros = vencs.Where(v => v.Documento != null && !v.Documento.EsCompra).Sum(v => v.Importe);
            decimal totalPagos = vencs.Where(v => v.Documento == null || v.Documento.EsCompra).Sum(v => v.Importe)
                + remesasPend.Where(r => r.Tipo == ERP.Domain.Entities.Bancario.TipoRemesaSEPA.Transferencia).Sum(r => r.ImporteTotal);

            // Buckets semanales
            var buckets = new List<object>();
            decimal acumulado = 0;
            for (var ini = hoy.AddDays(-(int)hoy.DayOfWeek + 1); ini <= hasta; ini = ini.AddDays(7))
            {
                var fin = ini.AddDays(6);
                var cob = vencs.Where(v => v.Documento != null && !v.Documento.EsCompra && v.FechaVencimiento >= ini && v.FechaVencimiento <= fin).Sum(v => v.Importe);
                var pag = vencs.Where(v => (v.Documento == null || v.Documento.EsCompra) && v.FechaVencimiento >= ini && v.FechaVencimiento <= fin).Sum(v => v.Importe)
                    + remesasPend.Where(r => r.Tipo == ERP.Domain.Entities.Bancario.TipoRemesaSEPA.Transferencia && r.FechaEjecucion >= ini && r.FechaEjecucion <= fin).Sum(r => r.ImporteTotal);
                acumulado += cob - pag;
                buckets.Add(new { Semana = ini.ToString("yyyy-MM-dd"), Cobros = cob, Pagos = pag, Neto = cob - pag, Acumulado = acumulado });
            }

            return Ok(new
            {
                Desde = hoy,
                Hasta = hasta,
                VencidoCobro = vencidoCobro,
                VencidoPago = vencidoPago,
                TotalCobrosPendientes = totalCobros,
                TotalPagosPendientes = totalPagos,
                SaldoPrevisto = totalCobros - totalPagos,
                Semanas = buckets
            });
        }
    }
}