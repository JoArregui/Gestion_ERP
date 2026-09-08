using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Services;
using Microsoft.AspNetCore.Authorization;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CicloFacturacionService _cicloService;
        private readonly PdfService _pdfService;

        public DocumentosController(
            ApplicationDbContext context, 
            CicloFacturacionService cicloService, 
            PdfService pdfService)
        {
            _context = context;
            _cicloService = cicloService;
            _pdfService = pdfService;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // GET: api/Documentos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentoComercial>>> GetDocumentos()
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<DocumentoComercial>());
            return await _context.Documentos
                .Where(d => d.EmpresaId == empresaId)
                .Include(d => d.Cliente)
                .Include(d => d.Proveedor)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();
        }

        // GET: api/Documentos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentoComercial>> GetDocumento(int id)
        {
            var documento = await _context.Documentos
                .Include(d => d.Lineas)
                .Include(d => d.Empresa)
                .Include(d => d.Cliente)
                .Include(d => d.Proveedor)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (documento == null) return NotFound();

            return documento;
        }

        // POST: api/Documentos
        [HttpPost]
        public async Task<ActionResult<DocumentoComercial>> PostDocumento(DocumentoComercial documento)
        {
            try
            {
                var nuevoDoc = await _cicloService.CrearDocumento(documento);
                return CreatedAtAction(nameof(GetDocumento), new { id = nuevoDoc.Id }, nuevoDoc);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Documentos/convertir/5?nuevoTipo=Factura
        [HttpPost("convertir/{id}")]
        public async Task<ActionResult<DocumentoComercial>> Convertir(int id, [FromQuery] TipoDocumento nuevoTipo)
        {
            try
            {
                var destino = await _cicloService.ConvertirDocumento(id, nuevoTipo);
                return CreatedAtAction(nameof(GetDocumento), new { id = destino.Id }, destino);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Documentos/5/pdf
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DescargarPdf(int id)
        {
            var doc = await _context.Documentos
                .Include(d => d.Lineas)
                .Include(d => d.Empresa)
                .Include(d => d.Cliente)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return NotFound("Documento no encontrado.");
            if (doc.EsCompra) return BadRequest("La generación de PDF solo está disponible para documentos de venta.");
            if (doc.Empresa == null) return BadRequest("Datos de la empresa emisora no encontrados.");

            try
            {
                var pdfBytes = _pdfService.GenerarFacturaPdf(doc, doc.Empresa, doc.Cliente);
                string nombreArchivo = $"{doc.Tipo}_{doc.NumeroDocumento}.pdf";
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar el PDF: {ex.Message}");
            }
        }

        // DELETE: api/Documentos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocumento(int id)
        {
            var documento = await _context.Documentos.FindAsync(id);
            if (documento == null) return NotFound();

            // Nota profesional: En ERPs reales se suele usar "Borrado Lógico" (isDeleted) 
            // en lugar de borrar físicamente si el documento tiene trazabilidad.
            if (documento.Tipo == TipoDocumento.Albaran)
            {
                try
                {
                    var eliminado = await _cicloService.IntentarEliminarAlbaran(id);
                    return eliminado ? NoContent() : NotFound();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            if (documento.Tipo is TipoDocumento.Factura or TipoDocumento.FacturaRectificativa)
                return BadRequest("Las facturas no se eliminan. Emita una factura rectificativa para mantener la trazabilidad contable.");

            if (await _context.Documentos.AnyAsync(d => d.DocumentoOrigenId == id))
                return BadRequest("No se puede eliminar un documento que ya ha generado documentos posteriores.");

            _context.Documentos.Remove(documento);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
