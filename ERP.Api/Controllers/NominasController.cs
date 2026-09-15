using Microsoft.AspNetCore.Mvc;
using ERP.Services;
using ERP.Domain.Entities;
using ERP.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NominasController : ControllerBase
    {
        private readonly NominaService _nominaService;
        private readonly ApplicationDbContext _context;
        private readonly PdfService _pdfService;

        public NominasController(NominaService nominaService, ApplicationDbContext context, PdfService pdfService)
        {
            _nominaService = nominaService;
            _context = context;
            _pdfService = pdfService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Nomina>>> GetNominas([FromQuery] int mes, [FromQuery] int anio)
        {
            return await _context.Nominas
                .Include(n => n.Empleado)
                .Where(n => n.Mes == mes && n.Anio == anio)
                .ToListAsync();
        }

        [HttpPost("generar/{empleadoId}")]
        public async Task<IActionResult> Generar(int empleadoId, [FromQuery] int mes, [FromQuery] int anio)
        {
            try 
            {
                var nomina = await _nominaService.GenerarNominaMensual(empleadoId, mes, anio);
                return Ok(nomina);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("pagar/{id}")]
        public async Task<IActionResult> Pagar(int id)
        {
            try 
            {
                var resultado = await _nominaService.ProcesarPagoNominaAsync(id);
                if (resultado) return Ok(new { message = "Pago procesado y Tesorería actualizada exitosamente." });
                return NotFound("La nómina no existe, el empleado no está vinculado o ya ha sido pagada.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> Pdf(int id)
        {
            var empresaId = int.TryParse(User.FindFirst("EmpresaId")?.Value, out var eid) ? eid : 0;
            if (empresaId == 0) return NotFound("Nómina no encontrada");
            var nomina = await _context.Nominas
                .AsNoTracking()
                .Include(n => n.Empleado)
                .FirstOrDefaultAsync(n => n.Id == id && n.Empleado != null && n.Empleado.EmpresaId == empresaId);
            if (nomina == null || nomina.Empleado == null) return NotFound("Nómina no encontrada");
            var empresa = await _context.Empresas.FindAsync(nomina.Empleado.EmpresaId);
            if (empresa == null) return NotFound("Empresa no encontrada");
            var pdf = _pdfService.GenerarNominaPdf(nomina, nomina.Empleado, empresa);
            return File(pdf, "application/pdf", $"NOMINA_{nomina.Anio}_{nomina.Mes:D2}_{nomina.Empleado.Apellidos}.pdf");
        }
    }
}