using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities;
using ERP.Data;
using Microsoft.EntityFrameworkCore;
using ERP.Services;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CicloFacturacionController : ControllerBase
    {
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
            return await _context.Documentos
                .Include(d => d.Empresa)
                .Include(d => d.Cliente)
                .Include(d => d.Lineas)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var eid) ? eid : 0;

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentoComercial>> GetDocumento(int id)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return NotFound();
            var doc = await _context.Documentos
                .AsNoTracking()
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
            if (id != doc.Id) return BadRequest(new { Message = "ID no coincide" });
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Unauthorized(new { Message = "Sesión sin empresa." });
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
                if (doc.EmpresaId == 0)
                {
                    var claim = User.FindFirst("EmpresaId")?.Value;
                    if (int.TryParse(claim, out var eid) && eid != 0) doc.EmpresaId = eid;
                    else return Unauthorized(new { Message = "Sesión sin empresa." });
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
            try{ await _cicloService.DesvincularFacturaAsync(id); return Ok(new { Message="Factura desvinculada. Edite el albarán y regenere."});}
            catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpPost("{id}/anular-interna")]
        public async Task<IActionResult> AnularInterna(int id)
        {
            try{ await _cicloService.AnularFacturaInternaAsync(id); return Ok(new { Message="Factura anulada internamente."});}
            catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpPost("{id}/rectificativa")]
        public async Task<IActionResult> Rectificativa(int id, [FromBody] RectificativaRequest req)
        {
            try{
                var rect = await _cicloService.CrearRectificativaAsync(id, req?.Motivo ?? $"Rectificativa de {id}", req?.Lineas);
                return Ok(rect);
            }catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        public class RectificativaRequest{ public string? Motivo{get;set;} public List<DocumentoLinea>? Lineas{get;set;}}
        [HttpPost("{id}/albaran-devolucion")]
        public async Task<IActionResult> AlbaranDevolucion(int id, [FromBody] List<DocumentoLinea> lineas)
        {
            try{ var alb = await _cicloService.CrearAlbaranDevolucionAsync(id, lineas); return Ok(alb);}catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpPost("{id}/marcar-enviada")]
        public async Task<IActionResult> MarcarEnviada(int id, [FromQuery] bool enviadaCliente=false, [FromQuery] bool presentadaHacienda=false)
        {
            try{ await _cicloService.MarcarEnviadaAsync(id, enviadaCliente, presentadaHacienda); return Ok(new { Message="Marcada."});}catch(Exception ex){ return BadRequest(new { Message=ex.Message});}
        }
        [HttpDelete("albaran/{id}")]
        public async Task<IActionResult> EliminarAlbaran(int id)
        {
            try
            {
                // Ahora el compilador encontrarÃ¡ el mÃ©todo correctamente
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