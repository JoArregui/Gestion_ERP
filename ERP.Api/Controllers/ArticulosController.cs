using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities;
using ERP.Domain.DTOs;
using ERP.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MiniExcelLibs;

namespace ERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ArticulosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ArticulosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper para obtener el EmpresaId del Token actual de forma segura
        private int GetEmpresaId()
        {
            var claim = User.FindFirst("EmpresaId")?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }

        // --- MÉTODOS DE IMPORTACIÓN Y EXCEL ---

        [HttpPost("importar")]
        public async Task<IActionResult> ImportarArticulos(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Archivo no válido.");

            int empresaId = GetEmpresaId();
            using var stream = file.OpenReadStream();
            
            // Leemos el Excel como una lista de diccionarios dinámicos
            var rows = stream.Query().ToList();
            int procesados = 0;
            int creados = 0;

            foreach (var row in rows)
            {
                // Mapeo flexible: intenta obtener valores por nombre de columna
                string codigo = row.Codigo?.ToString() ?? "";
                if (string.IsNullOrEmpty(codigo)) continue;

                string descripcion = row.Descripcion?.ToString() ?? "Artículo sin nombre";
                decimal precioCompra = Convert.ToDecimal(row.Costo ?? 0);
                decimal precioVenta = Convert.ToDecimal(row.PVP ?? 0);
                decimal stock = Convert.ToDecimal(row.Stock ?? 0);
                
                // Lógica Upsert: Buscar por Código y Empresa
                var articuloExistente = await _context.Articulos
                    .FirstOrDefaultAsync(a => a.Codigo == codigo && a.EmpresaId == empresaId);

                if (articuloExistente != null)
                {
                    // ACTUALIZAR EXISTENTE
                    articuloExistente.Descripcion = descripcion;
                    articuloExistente.PrecioCompra = precioCompra;
                    articuloExistente.PrecioVenta = precioVenta;
                    articuloExistente.Stock = stock;
                    articuloExistente.IsDescatalogado = false; // Re-activar si se vuelve a importar
                    _context.Articulos.Update(articuloExistente);
                }
                else
                {
                    // CREAR NUEVO
                    var nuevoArticulo = new Articulo
                    {
                        Codigo = codigo,
                        Descripcion = descripcion,
                        PrecioCompra = precioCompra,
                        PrecioVenta = precioVenta,
                        Stock = stock,
                        EmpresaId = empresaId,
                        FamiliaId = 1, // Por defecto asignar familia base
                        PorcentajeIva = 21,
                        IsDescatalogado = false
                    };
                    _context.Articulos.Add(nuevoArticulo);
                    creados++;
                }
                procesados++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Sincronización Excel finalizada. {procesados} procesados, {creados} nuevos." });
        }

        // --- MÉTODOS DE SINCRONIZACIÓN PARA SCANPAL (OFFLINE-FIRST) ---

        [HttpGet("scanpal/sincronizar")]
        public async Task<ActionResult> GetSincronizacionTotal()
        {
            int empresaId = GetEmpresaId();
            var articulos = await _context.Articulos
                .Where(a => a.EmpresaId == empresaId && !a.IsDescatalogado)
                .Select(a => new {
                    a.Id,
                    a.Codigo,
                    a.Descripcion,
                    a.Stock,
                    a.PrecioVenta,
                    a.PrecioCompra,
                    Familia = a.Familia != null ? a.Familia.Nombre : ""
                })
                .ToListAsync();

            return Ok(articulos);
        }

        [HttpGet("scanpal/documentos-pendientes")]
        public async Task<ActionResult> GetDocumentosPendientes()
        {
            int empresaId = GetEmpresaId();
            var documentos = await _context.Documentos
                .Where(d => d.EmpresaId == empresaId && !d.IsContabilizado && (d.Tipo == TipoDocumento.Albaran || d.Tipo == TipoDocumento.Pedido))
                .Select(d => new {
                    d.Id,
                    d.Tipo,
                    d.NumeroDocumento,
                    d.EsCompra,
                    d.Fecha,
                    Entidad = d.EsCompra ? (d.Proveedor != null ? d.Proveedor.RazonSocial : "Proveedor Desconocido") 
                                         : (d.Cliente != null ? d.Cliente.NombreComercial : "Cliente Desconocido"),
                    Lineas = d.Lineas.Select(l => new {
                        l.ArticuloId,
                        l.Cantidad,
                        CodigoArticulo = l.Articulo != null ? l.Articulo.Codigo : ""
                    })
                })
                .ToListAsync();

            return Ok(documentos);
        }

        [HttpPost("scanpal/subir-inventario")]
        public async Task<IActionResult> SincronizarInventarioMasivo([FromBody] List<AjusteStockDTO> lecturas)
        {
            if (lecturas == null || !lecturas.Any()) return BadRequest("No hay lecturas para procesar.");

            int empresaId = GetEmpresaId();
            var fechaSincro = DateTime.Now;

            foreach (var lectura in lecturas)
            {
                var articulo = await _context.Articulos
                    .FirstOrDefaultAsync(a => a.EmpresaId == empresaId && a.Codigo == lectura.CodigoBarras);

                if (articulo != null)
                {
                    decimal stockAnterior = articulo.Stock;
                    articulo.Stock = lectura.CantidadReal;

                    _context.Documentos.Add(new DocumentoComercial
                    {
                        Tipo = TipoDocumento.Albaran, 
                        Fecha = fechaSincro,
                        NumeroDocumento = $"INV-{fechaSincro:yyyyMMdd}-{lectura.TerminalId}",
                        EmpresaId = empresaId,
                        EsCompra = (lectura.CantidadReal > stockAnterior),
                        Observaciones = $"Sincro Scanpal (Inventario): {stockAnterior} -> {lectura.CantidadReal}",
                        IsContabilizado = true,
                        BaseImponible = 0, TotalIva = 0, Total = 0
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Sincronización masiva de inventario completada." });
        }

        // --- MÉTODOS CRUD ESTÁNDAR ---

        [HttpGet]
        public async Task<ActionResult> GetArticulos()
        {
            int empresaId = GetEmpresaId();
            
            // Usamos AsNoTracking y proyección anónima/DTO para evitar
            // excepciones por ciclos de referencia de Entity Framework al serializar JSON
            var articulos = await _context.Articulos
                .AsNoTracking()
                .Where(a => a.EmpresaId == empresaId)
                .Select(a => new
                {
                    a.Id,
                    a.Codigo,
                    a.Descripcion,
                    a.PrecioCompra,
                    a.PrecioVenta,
                    a.Stock,
                    a.PorcentajeIva,
                    a.IsDescatalogado,
                    a.EmpresaId,
                    a.FamiliaId,
                    FamiliaNombre = a.Familia != null ? a.Familia.Nombre : null,
                    a.ProveedorHabitualId,
                    ProveedorNombre = a.ProveedorHabitual != null ? a.ProveedorHabitual.RazonSocial : null
                })
                .ToListAsync();

            return Ok(articulos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Articulo>> GetArticulo(int id)
        {
            int empresaId = GetEmpresaId();
            var articulo = await _context.Articulos
                .AsNoTracking()
                .Include(a => a.Familia)
                .Include(a => a.ProveedorHabitual)
                .FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);

            if (articulo == null) return NotFound();
            return articulo;
        }

        [HttpGet("by-codigo/{codigo}")]
        public async Task<ActionResult<Articulo>> GetArticuloByCodigo(string codigo)
        {
            int empresaId = GetEmpresaId();
            var articulo = await _context.Articulos
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Codigo == codigo && a.EmpresaId == empresaId);

            if (articulo == null) return NotFound(new { message = "Artículo no encontrado" });
            return articulo;
        }

        [HttpPost]
        public async Task<ActionResult<Articulo>> PostArticulo(Articulo articulo)
        {
            articulo.EmpresaId = GetEmpresaId();
            _context.Articulos.Add(articulo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetArticulo), new { id = articulo.Id }, articulo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutArticulo(int id, Articulo articulo)
        {
            if (id != articulo.Id) return BadRequest();
            
            int empresaId = GetEmpresaId();
            if (articulo.EmpresaId != empresaId) return Unauthorized("No tiene permisos para modificar este artículo.");

            _context.Entry(articulo).State = EntityState.Modified;
            
            try 
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ArticuloExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticulo(int id)
        {
            int empresaId = GetEmpresaId();
            var articulo = await _context.Articulos
                .FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);

            if (articulo == null) return NotFound();
            
            // Soft delete: Marcamos como descatalogado
            articulo.IsDescatalogado = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("analisis-rentabilidad")]
        public async Task<ActionResult<IEnumerable<AnalisisRentabilidadDTO>>> GetRentabilidad()
        {
            int empresaId = GetEmpresaId();
            return await _context.Articulos
                .AsNoTracking()
                .Include(a => a.Familia)
                .Where(a => a.EmpresaId == empresaId && !a.IsDescatalogado)
                .Select(a => new AnalisisRentabilidadDTO
                {
                    Codigo = a.Codigo,
                    Descripcion = a.Descripcion,
                    Familia = a.Familia != null ? a.Familia.Nombre : "Sin Familia",
                    PrecioCompra = a.PrecioCompra,
                    PrecioVenta = a.PrecioVenta,
                    StockActual = a.Stock
                })
                .ToListAsync();
        }

        private bool ArticuloExists(int id) => _context.Articulos.Any(e => e.Id == id && e.EmpresaId == GetEmpresaId());
    }
}