using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Domain.Entities;
using ERP.Services;
using ERP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ERP.Api.Hubs;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FacturacionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FacturacionService _facturacionService;
        private readonly PdfService _pdfService;
        private readonly IHubContext<DashboardHub> _hubContext;

        public FacturacionController(
            ApplicationDbContext context, 
            FacturacionService facturacionService, 
            PdfService pdfService,
            IHubContext<DashboardHub> hubContext)
        {
            _context = context;
            _facturacionService = facturacionService;
            _pdfService = pdfService;
            _hubContext = hubContext;
        }

        [HttpGet("listado")]
        public async Task<IActionResult> GetFacturas()
        {
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (string.IsNullOrEmpty(empresaIdClaim)) return Unauthorized("Sesión inválida.");

            int empresaId = int.Parse(empresaIdClaim);

            var lista = await _context.Documentos
                .Include(d => d.Cliente)
                .Where(d => d.EmpresaId == empresaId && d.Tipo == TipoDocumento.Factura)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();

            return Ok(lista);
        }

        /// <summary>
        /// Cambiado de "guardar" a "crear-factura" para coincidir con NuevaVenta.razor
        /// </summary>
        [HttpPost("crear-factura")]
        public async Task<IActionResult> CrearFactura([FromBody] DocumentoComercial factura)
        {
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (string.IsNullOrEmpty(empresaIdClaim)) return Unauthorized("Sesión inválida.");
            
            factura.EmpresaId = int.Parse(empresaIdClaim);

            // El servicio gestiona: Guardado + Stock + MovimientoStock + Transaccionalidad
            var exito = await _facturacionService.RegistrarFacturaVentaAsync(factura);

            if (exito)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate");
                // Devolvemos el ID para que el frontend pueda generar el PDF/Ticket inmediatamente
                return Ok(new { Message = "Factura procesada y stock actualizado", Id = factura.Id });
            }

            return BadRequest("No se pudo procesar la factura. Revise el stock de los productos.");
        }

        [HttpGet("descargar-pdf/{id}")]
        public async Task<IActionResult> DescargarPdf(int id)
        {
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (!int.TryParse(empresaIdClaim, out var empresaId) || empresaId == 0 || string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value, "admin@erp.local", System.StringComparison.OrdinalIgnoreCase) || string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value, "admin@erp.com", System.StringComparison.OrdinalIgnoreCase)) return Forbid();
            var factura = await _context.Documentos
                .Include(d => d.Lineas)
                .Include(d => d.Cliente)
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);
            if (factura == null) return NotFound();
            var empresa = await _context.Empresas.FindAsync(factura.EmpresaId);
            
            // Generación de PDF A4 estándar
            byte[] pdfBytes = _pdfService.GenerarFacturaPdf(factura, empresa!, factura.Cliente!);
            
            // Retornamos el archivo directamente como stream para mayor eficiencia
            return File(pdfBytes, "application/pdf", $"FACTURA_{factura.NumeroDocumento}.pdf");
        }

        /// <summary>
        /// Endpoint para la impresión térmica de tickets (formato 80mm)
        /// </summary>
        [HttpGet("descargar-ticket/{id}")]
        public async Task<IActionResult> DescargarTicket(int id)
        {
            var empresaIdClaim2 = User.FindFirst("EmpresaId")?.Value;
            if (!int.TryParse(empresaIdClaim2, out var empresaId2) || empresaId2 == 0 || string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value, "admin@erp.local", System.StringComparison.OrdinalIgnoreCase) || string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value, "admin@erp.com", System.StringComparison.OrdinalIgnoreCase)) return Forbid();
            var factura = await _context.Documentos
                .Include(d => d.Lineas)
                .Include(d => d.Cliente)
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId2);
            if (factura == null) return NotFound();
            var empresa = await _context.Empresas.FindAsync(factura.EmpresaId);

            // Aquí llamamos a un método específico del PdfService para formato Ticket
            // Si no lo tienes, el PdfService debería tener una variante para tamaños pequeños
            byte[] pdfBytes = _pdfService.GenerarTicketPdf(factura, empresa!, factura.Cliente!);

            return File(pdfBytes, "application/pdf", $"TICKET_{factura.NumeroDocumento}.pdf");
        }
    }
}