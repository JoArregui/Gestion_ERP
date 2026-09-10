using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Services;
using ERP.Domain.Entities;
using ERP.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NominasController : ControllerBase
    {
        private readonly NominaService _nominaService;
        private readonly ApplicationDbContext _context;
        private readonly PdfService _pdfService;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.local", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.com", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.local", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.com", StringComparison.OrdinalIgnoreCase);

        public NominasController(NominaService nominaService, ApplicationDbContext context, PdfService pdfService)
        {
            _nominaService = nominaService;
            _context = context;
            _pdfService = pdfService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Nomina>>> GetNominas([FromQuery] int mes, [FromQuery] int anio)
        {
            if (IsGeneric) return Ok(new List<Nomina>());
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<Nomina>());
            return await _context.Nominas
                .Include(n => n.Empleado)
                .Where(n => n.Empleado != null && n.Empleado.EmpresaId == empresaId && n.Mes == mes && n.Anio == anio)
                .ToListAsync();
        }

        [HttpPost("generar/{empleadoId}")]
        public async Task<IActionResult> Generar(int empleadoId, [FromQuery] int mes, [FromQuery] int anio)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var empOk = await _context.Empleados.AnyAsync(e => e.Id == empleadoId && e.EmpresaId == empresaId);
            if (!empOk) return Forbid();
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
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var nominaCheck = await _context.Nominas.Include(n=>n.Empleado).FirstOrDefaultAsync(n=>n.Id==id);
            if (nominaCheck == null) return NotFound();
            if (nominaCheck.Empleado == null || nominaCheck.Empleado.EmpresaId != empresaId) return Forbid();
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
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var nomina = await _context.Nominas.Include(n => n.Empleado).FirstOrDefaultAsync(n => n.Id == id && n.Empleado != null && n.Empleado.EmpresaId == empresaId);
            if (nomina == null || nomina.Empleado == null) return NotFound("Nómina no encontrada");
            var empresa = await _context.Empresas.FindAsync(nomina.Empleado.EmpresaId);
            if (empresa == null) return NotFound("Empresa no encontrada");
            var pdf = _pdfService.GenerarNominaPdf(nomina, nomina.Empleado, empresa);
            return File(pdf, "application/pdf", $"NOMINA_{nomina.Anio}_{nomina.Mes:D2}_{nomina.Empleado.Apellidos}.pdf");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Nomina dto)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var nomina = await _context.Nominas.Include(n => n.Empleado).FirstOrDefaultAsync(n => n.Id == id && n.Empleado != null && n.Empleado.EmpresaId == empresaId);
            if (nomina == null) return NotFound("Nómina no encontrada");
            if (nomina.EstaPagada) return BadRequest(new { Message = "No se puede modificar una nómina ya pagada. Anule el pago primero." });
            // Campos editables
            nomina.SalarioBase = dto.SalarioBase;
            nomina.Complementos = dto.Complementos;
            nomina.HorasExtra = dto.HorasExtra;
            nomina.PagasExtraProrrateadas = dto.PagasExtraProrrateadas;
            nomina.BaseContingenciasComunes = dto.BaseContingenciasComunes;
            nomina.BaseContingenciasProfesionales = dto.BaseContingenciasProfesionales;
            nomina.BaseHorasExtra = dto.BaseHorasExtra;
            nomina.CuotaSegSocialTrabajador = dto.CuotaSegSocialTrabajador;
            nomina.TipoIRPF = dto.TipoIRPF;
            nomina.RetencionIRPF = dto.RetencionIRPF;
            nomina.Deducciones = dto.Deducciones;
            nomina.ImportePagasExtra = dto.ImportePagasExtra;
            await _context.SaveChangesAsync();
            return Ok(nomina);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            var nomina = await _context.Nominas.Include(n => n.Empleado).FirstOrDefaultAsync(n => n.Id == id && n.Empleado != null && n.Empleado.EmpresaId == empresaId);
            if (nomina == null) return NotFound("Nómina no encontrada");
            if (nomina.EstaPagada) return BadRequest(new { Message = "No se puede eliminar una nómina pagada. Genere una rectificativa." });
            _context.Nominas.Remove(nomina);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}