using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Domain.Entities;
using ERP.Data;
using Microsoft.EntityFrameworkCore;
using ERP.Services;
using System.Security.Claims;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CicloFacturacionController : ControllerBase
    {
        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User);

        private readonly CicloFacturacionService _cicloService;
        private readonly ApplicationDbContext _context;

        public CicloFacturacionController(CicloFacturacionService cicloService, ApplicationDbContext context)
        {
            _cicloService = cicloService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentoComercial>>> GetDocumentos()
        {
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (!int.TryParse(empresaIdClaim, out var empresaId) || empresaId == 0)
                return Ok(new List<DocumentoComercial>());
            return await _context.Documentos
                .Where(d => d.EmpresaId == empresaId)
                .Include(d => d.Empresa)
                .Include(d => d.Cliente)
                .Include(d => d.Lineas)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentoComercial>> GetDocumento(int id)
        {
            var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
            if (!int.TryParse(empresaIdClaim, out var empresaId) || empresaId == 0)
                return Forbid();
            var doc = await _context.Documentos
                .Include(d => d.Empresa)
                .Include(d => d.Cliente)
                .Include(d => d.Lineas)
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);
            if (doc == null) return NotFound();
            return doc;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutDocumento(int id, [FromBody] DocumentoComercial doc)
        {
            if (IsGeneric) return Forbid();
            var empresaId = GetEmpresaId();
            if (id != doc.Id) return BadRequest(new { Message = "ID no coincide" });
            var existente = await _context.Documentos.Include(d => d.Lineas).FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);
            if (existente == null) return NotFound(new { Message = "Documento no encontrado" });
            if (existente.EstaEmitidaFormalmente || existente.Tipo == TipoDocumento.FacturaRectificativa)
                return BadRequest(new { Message = "Factura no editable por normativa. Genere una rectificativa o anulación." });
            existente.ClienteId = doc.ClienteId;
            existente.Fecha = doc.Fecha;
            existente.Observaciones = doc.Observaciones;
            existente.MetodoPago = doc.MetodoPago;
            existente.BaseImponible = doc.BaseImponible;
            existente.TotalIva = doc.TotalIva;
            existente.Total = doc.Total;
            _context.DocumentoLineas.RemoveRange(existente.Lineas);
            await _context.SaveChangesAsync();
            foreach (var l in doc.Lineas)
            {
                _context.DocumentoLineas.Add(new DocumentoLinea
                {
                    DocumentoId = existente.Id,
                    ArticuloId = l.ArticuloId,
                    DescripcionArticulo = l.DescripcionArticulo,
                    Cantidad = l.Cantidad,
                    PrecioUnitario = l.PrecioUnitario,
                    PorcentajeIva = l.PorcentajeIva,
                    CategoriaNombre = l.CategoriaNombre
                });
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("guardar")]
        public async Task<ActionResult<DocumentoComercial>> Guardar([FromBody] DocumentoComercial doc)
        {
            try
            {
                // Usar la EmpresaId del documento si viene especificada y el usuario tiene acceso a ella
                // (permite facturar por una empresa distinta a la primaria del JWT)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId!);
                if (user == null) return Unauthorized();

                var userCompanies = new List<int>();
                if (user.EmpresaId.HasValue) userCompanies.Add(user.EmpresaId.Value);
                userCompanies.AddRange(await _context.UserEmpresas.Where(ue => ue.UserId == user.Id).Select(ue => ue.EmpresaId).ToListAsync());
                userCompanies = userCompanies.Distinct().ToList();

                if (doc.EmpresaId > 0 && userCompanies.Contains(doc.EmpresaId))
                {
                    doc.EmpresaId = doc.EmpresaId.Value;
                }
                else
                {
                    // Fallback a la empresa principal del JWT
                    var claim = User.FindFirst("EmpresaId")?.Value;
                    if (!int.TryParse(claim, out var eid) || eid == 0)
                        return Unauthorized("Sesi�n sin empresa � complete el onboarding.");
                    doc.EmpresaId = eid;
                }

                if (doc.Tipo == 0) doc.Tipo = TipoDocumento.Presupuesto;
                var creado = await _cicloService.CrearDocumento(doc);
                return Ok(creado);
            }
            catch (Exception ex)
            {
                // No se expone la cadena de InnerExceptions (fuga de detalles técnicos/SQL).
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/convertir")]
        public async Task<ActionResult<DocumentoComercial>> Convertir(int id, [FromQuery] TipoDocumento nuevoTipo)
        {
            if (IsGeneric) return Forbid();
            var eid = GetEmpresaId();
            var docCheck = await _context.Documentos.AnyAsync(d => d.Id == id && d.EmpresaId == eid);
            if (!docCheck) return NotFound();
            try
            {
                var documentoNuevo = await _cicloService.ConvertirDocumento(id, nuevoTipo);
                return Ok(documentoNuevo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

                [HttpPost("{id}/desvincular")]
        public async Task<IActionResult> Desvincular(int id)
        {
            if (IsGeneric) return Forbid();
            var eid2 = GetEmpresaId(); if (!await _context.Documentos.AnyAsync(d=>d.Id==id && d.EmpresaId==eid2)) return NotFound();
            try{ await _cicloService.DesvincularFacturaAsync(id); return Ok(new { Message="Factura desvinculada. Edite el albar�n y regenere."});}
            catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpPost("{id}/anular-interna")]
        public async Task<IActionResult> AnularInterna(int id)
        {
            if (IsGeneric) return Forbid();
            var eid3 = GetEmpresaId(); if (!await _context.Documentos.AnyAsync(d=>d.Id==id && d.EmpresaId==eid3)) return NotFound();
            try{ await _cicloService.AnularFacturaInternaAsync(id); return Ok(new { Message="Factura anulada internamente."});}
            catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpPost("{id}/rectificativa")]
        public async Task<IActionResult> Rectificativa(int id, [FromBody] RectificativaRequest req)
        {
            if (IsGeneric) return Forbid();
            var eid4 = GetEmpresaId(); if (!await _context.Documentos.AnyAsync(d=>d.Id==id && d.EmpresaId==eid4)) return NotFound();
            try{
                var rect = await _cicloService.CrearRectificativaAsync(id, req?.Motivo ?? $"Rectificativa de {id}", req?.Lineas);
                return Ok(rect);
            }catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        public class RectificativaRequest{ public string? Motivo{get;set;} public List<DocumentoLinea>? Lineas{get;set;}}
        [HttpPost("{id}/albaran-devolucion")]
        public async Task<IActionResult> AlbaranDevolucion(int id, [FromBody] List<DocumentoLinea> lineas)
        {
            if (IsGeneric) return Forbid();
            var eid5 = GetEmpresaId(); if (!await _context.Documentos.AnyAsync(d=>d.Id==id && d.EmpresaId==eid5)) return NotFound();
            try{ var alb = await _cicloService.CrearAlbaranDevolucionAsync(id, lineas); return Ok(alb);}catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpPost("{id}/marcar-enviada")]
        public async Task<IActionResult> MarcarEnviada(int id, [FromQuery] bool enviadaCliente=false, [FromQuery] bool presentadaHacienda=false)
        {
            if (IsGeneric) return Forbid();
            var eid6 = GetEmpresaId(); if (!await _context.Documentos.AnyAsync(d=>d.Id==id && d.EmpresaId==eid6)) return NotFound();
            try{ await _cicloService.MarcarEnviadaAsync(id, enviadaCliente, presentadaHacienda); return Ok(new { Message="Marcada."});}catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpDelete("albaran/{id}")]
        public async Task<IActionResult> EliminarAlbaran(int id)
        {
            if (IsGeneric) return Forbid();
            var eid7 = GetEmpresaId(); if (!await _context.Documentos.AnyAsync(d=>d.Id==id && d.EmpresaId==eid7)) return NotFound();
            try
            {
                // Ahora el compilador encontrará el método correctamente
                var exito = await _cicloService.IntentarEliminarAlbaran(id);
                
                if (exito)
                {
                    return Ok(new { Message = "Albarán eliminado y stock liberado correctamente." });
                }
                
                return NotFound(new { Message = "El albarán no existe." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}