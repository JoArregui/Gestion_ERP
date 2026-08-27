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
                .Include(d => d.Cliente)
                .Include(d => d.Lineas)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentoComercial>> GetDocumento(int id)
        {
            var doc = await _context.Documentos
                .Include(d => d.Cliente)
                .Include(d => d.Lineas)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (doc == null) return NotFound();
            return doc;
        }

        [HttpPost("guardar")]
        public async Task<ActionResult<DocumentoComercial>> Guardar([FromBody] DocumentoComercial doc)
        {
            try
            {
                // Si no viene EmpresaId (Web no lo envía), lo tomamos del JWT
                if (doc.EmpresaId == 0)
                {
                    var claim = User.FindFirst("EmpresaId")?.Value;
                    if (int.TryParse(claim, out var eid)) doc.EmpresaId = eid;
                    else doc.EmpresaId = await _context.Empresas.Select(e => e.Id).FirstOrDefaultAsync();
                }
                // El frontend envía Tipo según la ruta (Presupuesto/Factura/Albarán)
                if (doc.Tipo == 0) doc.Tipo = TipoDocumento.Presupuesto;
                var creado = await _cicloService.CrearDocumento(doc);
                return Ok(creado);
            }
            catch (Exception ex)
            {
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

        [HttpDelete("albaran/{id}")]
        public async Task<IActionResult> EliminarAlbaran(int id)
        {
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