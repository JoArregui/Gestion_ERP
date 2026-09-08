using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities.Trazabilidad;
using ERP.Domain.DTOs.Trazabilidad;
using ERP.Services.Trazabilidad;

namespace ERP.Api.Controllers.Trazabilidad
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrazabilidadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TrazabilidadService _svc;
        public TrazabilidadController(ApplicationDbContext context, TrazabilidadService svc) { _context = context; _svc = svc; }
        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Lotes ───────────────────────────────────────────────────────
        [HttpGet("lotes")]
        public async Task<ActionResult<IEnumerable<LoteTrazabilidad>>> GetLotes([FromQuery] int? articuloId = null, [FromQuery] EstadoLote? estado = null, [FromQuery] bool? proximosCaducar = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.LotesTrazabilidad.Include(l => l.Articulo).Where(l => l.EmpresaId == empresaId);
            if (articuloId.HasValue) q = q.Where(l => l.ArticuloId == articuloId.Value);
            if (estado.HasValue) q = q.Where(l => l.Estado == estado.Value);
            if (proximosCaducar == true) q = q.Where(l => l.FechaCaducidad != null && l.FechaCaducidad <= DateTime.Now.AddDays(30) && l.FechaCaducidad > DateTime.Now);
            return Ok(await q.OrderByDescending(l => l.FechaCreacion).ToListAsync());
        }

        [HttpGet("lotes/{id}")]
        public async Task<ActionResult<LoteTrazabilidad>> GetLote(int id)
        {
            var l = await _context.LotesTrazabilidad.Include(x => x.Articulo).Include(x => x.MovimientosEntrada).Include(x => x.MovimientosSalida).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return l == null ? NotFound() : Ok(l);
        }

        [HttpGet("lotes/codigo/{codigo}")]
        public async Task<ActionResult<LoteTrazabilidad>> GetPorCodigo(string codigo)
        {
            var l = await _context.LotesTrazabilidad.FirstOrDefaultAsync(x => x.CodigoLote == codigo && x.EmpresaId == GetEmpresaId());
            return l == null ? NotFound(new { Message = "Lote no encontrado" }) : Ok(l);
        }

        [HttpPost("lotes")]
        public async Task<ActionResult<LoteDto>> PostLote(CrearLoteDto dto)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            var entity = new LoteTrazabilidad
            {
                EmpresaId = empresaId, CodigoLote = dto.CodigoLote, ArticuloId = dto.ArticuloId, ProveedorId = dto.ProveedorId,
                LoteProveedor = dto.LoteProveedor, DocumentoOrigen = dto.DocumentoOrigen, FechaRecepcion = dto.FechaRecepcion,
                CantidadInicial = dto.CantidadInicial, FechaProduccion = dto.FechaProduccion, FechaCaducidad = dto.FechaCaducidad,
                FechaConsumoPreferente = dto.FechaConsumoPreferente, CondicionesAlmacenamiento = dto.CondicionesAlmacenamiento,
                TemperaturaMinima = dto.TemperaturaMinima, TemperaturaMaxima = dto.TemperaturaMaxima,
                EsPCC = dto.EsPCC, ParametrosCriticos = dto.ParametrosCriticos
            };
            try
            {
                var creado = await _svc.CrearLoteAsync(entity, User.Identity?.Name);
                return CreatedAtAction(nameof(GetLote), new { id = creado.Id }, new LoteDto { Id = creado.Id, CodigoLote = creado.CodigoLote, ArticuloId = creado.ArticuloId, CantidadInicial = creado.CantidadInicial, CantidadActual = creado.CantidadActual, CantidadDisponible = creado.CantidadDisponible, FechaProduccion = creado.FechaProduccion, FechaCaducidad = creado.FechaCaducidad, Estado = creado.Estado.ToString(), EsPCC = creado.EsPCC });
            }
            catch (InvalidOperationException ex) { return Conflict(new { Message = ex.Message }); }
        }

        [HttpPut("lotes/{id}")]
        public async Task<IActionResult> PutLote(int id, LoteTrazabilidad dto)
        {
            if (id != dto.Id) return BadRequest();
            var existente = await _context.LotesTrazabilidad.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (existente == null) return NotFound();
            existente.CantidadActual = dto.CantidadActual; existente.Estado = dto.Estado;
            existente.FechaCaducidad = dto.FechaCaducidad; existente.CondicionesAlmacenamiento = dto.CondicionesAlmacenamiento;
            existente.TieneAnalisisOficial = dto.TieneAnalisisOficial; existente.FechaModificacion = DateTime.Now;
            existente.UsuarioModificacion = User.Identity?.Name;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("lotes/{id}/trazabilidad-completa")]
        public async Task<ActionResult> GetTrazabilidadCompleta(int id)
        {
            var lote = await _context.LotesTrazabilidad.Include(l => l.Articulo).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (lote == null) return NotFound();
            var movimientos = await _context.MovimientosLote.Where(m => m.LoteId == id && m.EmpresaId == GetEmpresaId()).OrderBy(m => m.Fecha).ToListAsync();
            var alertas = await _context.AlertasTrazabilidad.Where(a => a.LoteId == id).ToListAsync();
            var retiradas = await _context.RetiradasLote.Where(r => r.LoteId == id).ToListAsync();
            // Un paso atrás / adelante
            var pasoAtras = new { lote.ProveedorId, lote.LoteProveedor, lote.DocumentoOrigen, lote.FechaRecepcion };
            var pasoAdelante = await _context.MovimientosLote.Where(m => m.LoteId == id && m.Tipo == TipoMovimientoLote.Expedicion).Select(m => new { m.ClienteId, m.NumeroDocumento, m.FechaEntrega }).ToListAsync();
            return Ok(new { Lote = lote, Movimientos = movimientos, Alertas = alertas, Retiradas = retiradas, UnPasoAtras = pasoAtras, UnPasoAdelante = pasoAdelante });
        }

        // ── Movimientos ─────────────────────────────────────────────────
        [HttpGet("movimientos")]
        public async Task<ActionResult<IEnumerable<MovimientoLote>>> GetMovimientos([FromQuery] int? loteId = null, [FromQuery] TipoMovimientoLote? tipo = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.MovimientosLote.Where(m => m.EmpresaId == empresaId);
            if (loteId.HasValue) q = q.Where(m => m.LoteId == loteId.Value);
            if (tipo.HasValue) q = q.Where(m => m.Tipo == tipo.Value);
            return Ok(await q.OrderByDescending(m => m.Fecha).ToListAsync());
        }

        [HttpPost("movimientos")]
        public async Task<ActionResult<MovimientoLote>> PostMovimiento(MovimientoLote dto)
        {
            dto.EmpresaId = GetEmpresaId();
            var lote = await _context.LotesTrazabilidad.FirstOrDefaultAsync(l => l.Id == dto.LoteId && l.EmpresaId == dto.EmpresaId);
            if (lote == null) return BadRequest(new { Message = "Lote no válido" });

            // Actualizar stock del lote según tipo
            if (dto.Tipo == TipoMovimientoLote.Recepcion || dto.Tipo == TipoMovimientoLote.Devolucion || dto.Tipo == TipoMovimientoLote.Produccion)
                lote.CantidadActual += dto.Cantidad;
            else if (dto.Tipo == TipoMovimientoLote.Expedicion || dto.Tipo == TipoMovimientoLote.Merma || dto.Tipo == TipoMovimientoLote.Destruccion || dto.Tipo == TipoMovimientoLote.Retirada)
            {
                if (lote.CantidadDisponible < dto.Cantidad) return BadRequest(new { Message = $"Stock insuficiente. Disponible {lote.CantidadDisponible}" });
                lote.CantidadActual -= dto.Cantidad;
                if (lote.CantidadActual <= 0) lote.Estado = EstadoLote.Agotado;
            }

            dto.FechaCreacion = DateTime.Now; dto.UsuarioCreacion = User.Identity?.Name;
            _context.MovimientosLote.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMovimientos), new { id = dto.Id }, dto);
        }

        // ── Alertas ─────────────────────────────────────────────────────
        [HttpGet("alertas")]
        public async Task<ActionResult<IEnumerable<AlertaTrazabilidad>>> GetAlertas([FromQuery] bool? soloPendientes = true, [FromQuery] SeveridadAlerta? severidad = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.AlertasTrazabilidad.Include(a => a.Lote).Where(a => a.EmpresaId == empresaId);
            if (soloPendientes == true) q = q.Where(a => !a.Resuelta);
            if (severidad.HasValue) q = q.Where(a => a.Severidad == severidad.Value);
            return Ok(await q.OrderByDescending(a => a.FechaAlerta).ToListAsync());
        }

        [HttpPost("alertas/{id}/leer")]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var a = await _context.AlertasTrazabilidad.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (a == null) return NotFound();
            a.Leida = true; a.FechaLectura = DateTime.Now; a.UsuarioLectura = User.Identity?.Name;
            await _context.SaveChangesAsync();
            return Ok(a);
        }

        [HttpPost("alertas/{id}/resolver")]
        public async Task<IActionResult> Resolver(int id, [FromBody] string accionCorrectiva)
        {
            var a = await _context.AlertasTrazabilidad.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (a == null) return NotFound();
            a.Resuelta = true; a.FechaResolucion = DateTime.Now; a.UsuarioResolucion = User.Identity?.Name; a.AccionCorrectiva = accionCorrectiva;
            await _context.SaveChangesAsync();
            return Ok(a);
        }

        [HttpPost("alertas/generar-caducidades")]
        public async Task<ActionResult> GenerarAlertasCaducidad()
        {
            var empresaId = GetEmpresaId();
            var lotes = await _context.LotesTrazabilidad.Where(l => l.EmpresaId == empresaId && l.Estado == EstadoLote.Activo && l.FechaCaducidad != null && l.FechaCaducidad <= DateTime.Now.AddDays(30)).ToListAsync();
            int creadas = 0;
            foreach (var lote in lotes)
            {
                var existe = await _context.AlertasTrazabilidad.AnyAsync(a => a.LoteId == lote.Id && a.Tipo == TipoAlertaTrazabilidad.ProximaCaducidad && !a.Resuelta);
                if (!existe)
                {
                    _context.AlertasTrazabilidad.Add(new AlertaTrazabilidad
                    {
                        EmpresaId = empresaId, LoteId = lote.Id,
                        Tipo = lote.EstaCaducado ? TipoAlertaTrazabilidad.Caducado : TipoAlertaTrazabilidad.ProximaCaducidad,
                        Severidad = lote.EstaCaducado ? SeveridadAlerta.Critica : SeveridadAlerta.Alta,
                        Titulo = lote.EstaCaducado ? $"Lote {lote.CodigoLote} caducado" : $"Lote {lote.CodigoLote} próximo a caducar",
                        Descripcion = $"Caducidad: {lote.FechaCaducidad:dd/MM/yyyy}"
                    });
                    creadas++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { Creadas = creadas });
        }

        // ── Retiradas (RASFF) ───────────────────────────────────────────
        [HttpGet("retiradas")]
        public async Task<ActionResult<IEnumerable<RetiradaLote>>> GetRetiradas([FromQuery] EstadoRetirada? estado = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.RetiradasLote.Include(r => r.Lote).Where(r => r.EmpresaId == empresaId);
            if (estado.HasValue) q = q.Where(r => r.Estado == estado.Value);
            return Ok(await q.OrderByDescending(r => r.FechaDeteccion).ToListAsync());
        }

        [HttpGet("retiradas/{id}")]
        public async Task<ActionResult<RetiradaLote>> GetRetirada(int id)
        {
            var r = await _context.RetiradasLote.Include(x => x.Lote).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPost("retiradas")]
        public async Task<ActionResult<RetiradaLote>> PostRetirada(RetiradaLote dto)
        {
            dto.EmpresaId = GetEmpresaId();
            var lote = await _context.LotesTrazabilidad.FirstOrDefaultAsync(l => l.Id == dto.LoteId && l.EmpresaId == dto.EmpresaId);
            if (lote == null) return BadRequest(new { Message = "Lote no válido" });
            dto.FechaDeteccion = dto.FechaDeteccion == default ? DateTime.Now : dto.FechaDeteccion;
            dto.FechaCreacion = DateTime.Now; dto.UsuarioResponsable = User.Identity?.Name;
            // Bloquear lote automáticamente
            lote.Estado = EstadoLote.Retirado;
            _context.RetiradasLote.Add(dto);
            // Alerta crítica
            _context.AlertasTrazabilidad.Add(new AlertaTrazabilidad
            {
                EmpresaId = dto.EmpresaId, LoteId = dto.LoteId,
                Tipo = TipoAlertaTrazabilidad.RetiradaMercado, Severidad = SeveridadAlerta.Critica,
                Titulo = $"Retirada {dto.Severidad} lote {lote.CodigoLote}",
                Descripcion = dto.Motivo
            });
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRetirada), new { id = dto.Id }, dto);
        }

        [HttpPost("retiradas/{id}/notificar-aesan")]
        public async Task<IActionResult> NotificarAESAN(int id, [FromBody] string numeroExpediente)
        {
            var r = await _context.RetiradasLote.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (r == null) return NotFound();
            r.FechaNotificacionAutoridades = DateTime.Now; r.NumeroExpedienteAESAN = numeroExpediente;
            if (r.Severidad == SeveridadRetirada.ClaseI) r.NumeroNotificacionRASFF = $"RASFF-{DateTime.Now:yyyy}-{r.Id:D6}";
            await _context.SaveChangesAsync();
            return Ok(new { r.NumeroExpedienteAESAN, r.NumeroNotificacionRASFF });
        }

        [HttpPost("retiradas/{id}/cerrar")]
        public async Task<IActionResult> CerrarRetirada(int id, [FromBody] string acciones)
        {
            var r = await _context.RetiradasLote.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (r == null) return NotFound();
            r.Estado = EstadoRetirada.Cerrada; r.FechaCierre = DateTime.Now; r.AccionesCorrectivas = acciones;
            await _context.SaveChangesAsync();
            return Ok(r);
        }
    }
}
