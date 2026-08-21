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