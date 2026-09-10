using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
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

            // Actualización de estado profesional
            vencimiento.Estado = "Pagado";
            vencimiento.FechaPago = DateTime.Now;
            vencimiento.MetodoPago = string.IsNullOrEmpty(metodoPago) ? "Efectivo/Caja" : metodoPago;

            await _context.SaveChangesAsync();

            return Ok(new { 
                Status = "Success", 
                Message = $"Vencimiento {id} marcado como pagado el {vencimiento.FechaPago}." 
            });
        }
    }
}