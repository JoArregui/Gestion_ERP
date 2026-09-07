using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities.Fiscal;

namespace ERP.Api.Controllers.Fiscal
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FiscalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public FiscalController(ApplicationDbContext context) => _context = context;
        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Configuración IVA ───────────────────────────────────────────
        [HttpGet("configuracion")]
        public async Task<ActionResult<IEnumerable<ConfiguracionIVA>>> GetConfigs()
            => Ok(await _context.ConfiguracionesIVA.Where(c => c.EmpresaId == GetEmpresaId()).ToListAsync());

        [HttpGet("configuracion/{id}")]
        public async Task<ActionResult<ConfiguracionIVA>> GetConfig(int id)
        {
            var c = await _context.ConfiguracionesIVA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return c == null ? NotFound() : Ok(c);
        }

        [HttpGet("configuracion/ejercicio/{ejercicioId}")]
        public async Task<ActionResult<ConfiguracionIVA>> GetPorEjercicio(int ejercicioId)
        {
            var c = await _context.ConfiguracionesIVA.FirstOrDefaultAsync(x => x.EjercicioId == ejercicioId && x.EmpresaId == GetEmpresaId());
            return c == null ? NotFound(new { Message = "Sin configuración para ese ejercicio" }) : Ok(c);
        }

        [HttpPost("configuracion")]
        public async Task<ActionResult<ConfiguracionIVA>> PostConfig(ConfiguracionIVA dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            // Validar unicidad por empresa+ejercicio
            if (await _context.ConfiguracionesIVA.AnyAsync(x => x.EmpresaId == dto.EmpresaId && x.EjercicioId == dto.EjercicioId))
                return Conflict(new { Message = "Ya existe configuración para ese ejercicio" });
            // Calcular prorrata redondeada techo si aplica
            if (dto.AplicaProrrataGeneral && dto.PorcentajeProrrataGeneral.HasValue)
                dto.PorcentajeProrrataGeneralRedondeado = Math.Ceiling(dto.PorcentajeProrrataGeneral.Value);
            dto.FechaCreacion = DateTime.Now;
            _context.ConfiguracionesIVA.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetConfig), new { id = dto.Id }, dto);
        }

        [HttpPut("configuracion/{id}")]
        public async Task<IActionResult> PutConfig(int id, ConfiguracionIVA dto)
        {
            if (id != dto.Id) return BadRequest();
            var existente = await _context.ConfiguracionesIVA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (existente == null) return NotFound();
            existente.AplicaProrrataGeneral = dto.AplicaProrrataGeneral; existente.PorcentajeProrrataGeneral = dto.PorcentajeProrrataGeneral;
            if (dto.AplicaProrrataGeneral && dto.PorcentajeProrrataGeneral.HasValue)
                existente.PorcentajeProrrataGeneralRedondeado = Math.Ceiling(dto.PorcentajeProrrataGeneral.Value);
            existente.AplicaProrrataEspecial = dto.AplicaProrrataEspecial; existente.DetalleProrrataEspecialJson = dto.DetalleProrrataEspecialJson;
            existente.TieneSectoresDiferenciados = dto.TieneSectoresDiferenciados; existente.SectoresJson = dto.SectoresJson;
            existente.SujetoRecargoEquivalencia = dto.SujetoRecargoEquivalencia; existente.RecargoGeneral = dto.RecargoGeneral;
            existente.RecargoReducido = dto.RecargoReducido; existente.RecargoSuperreducido = dto.RecargoSuperreducido; existente.RecargoTabaco = dto.RecargoTabaco;
            existente.AplicaIVACaja = dto.AplicaIVACaja; existente.InversionSujetoPasivoHabitual = dto.InversionSujetoPasivoHabitual;
            existente.RegimenAgenciasViajes = dto.RegimenAgenciasViajes; existente.RegimenBienesUsados = dto.RegimenBienesUsados;
            existente.RegimenObjetosArte = dto.RegimenObjetosArte; existente.RegimenOroInversion = dto.RegimenOroInversion;
            existente.FechaUltimaActualizacion = DateTime.Now; existente.UsuarioModificacion = User.Identity?.Name;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── Tarifas (IGIC/IPSI/IVA) ─────────────────────────────────────
        [HttpGet("tarifas")]
        public async Task<ActionResult<IEnumerable<TarifaImpuesto>>> GetTarifas([FromQuery] TerritorioFiscal? territorio = null, [FromQuery] bool? soloVigentes = true)
        {
            var q = _context.TarifasImpuesto.AsQueryable();
            if (territorio.HasValue) q = q.Where(t => t.Territorio == territorio.Value);
            if (soloVigentes == true) q = q.Where(t => t.Vigente && (t.FechaHasta == null || t.FechaHasta > DateTime.Now));
            return Ok(await q.OrderBy(t => t.Territorio).ThenBy(t => t.Porcentaje).ToListAsync());
        }

        [HttpGet("tarifas/{id}")]
        public async Task<ActionResult<TarifaImpuesto>> GetTarifa(int id)
        {
            var t = await _context.TarifasImpuesto.FindAsync(id);
            return t == null ? NotFound() : Ok(t);
        }

        [HttpPost("tarifas")]
        public async Task<ActionResult<TarifaImpuesto>> PostTarifa(TarifaImpuesto dto)
        {
            dto.FechaDesde = dto.FechaDesde == default ? new DateTime(2026, 1, 1) : dto.FechaDesde;
            _context.TarifasImpuesto.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTarifa), new { id = dto.Id }, dto);
        }

        [HttpPut("tarifas/{id}")]
        public async Task<IActionResult> PutTarifa(int id, TarifaImpuesto dto)
        {
            if (id != dto.Id) return BadRequest();
            var e = await _context.TarifasImpuesto.FindAsync(id);
            if (e == null) return NotFound();
            e.Nombre = dto.Nombre; e.Porcentaje = dto.Porcentaje; e.RecargoEquivalencia = dto.RecargoEquivalencia;
            e.Vigente = dto.Vigente; e.FechaHasta = dto.FechaHasta;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── Sectores diferenciados ──────────────────────────────────────
        [HttpGet("sectores")]
        public async Task<ActionResult<IEnumerable<SectorDiferenciadoIVA>>> GetSectores()
            => Ok(await _context.SectoresDiferenciadosIVA.Where(s => s.EmpresaId == GetEmpresaId() && s.Activo).ToListAsync());

        [HttpPost("sectores")]
        public async Task<ActionResult<SectorDiferenciadoIVA>> PostSector(SectorDiferenciadoIVA dto)
        {
            dto.EmpresaId = GetEmpresaId();
            _context.SectoresDiferenciadosIVA.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSectores), new { id = dto.Id }, dto);
        }

        // ── Liquidaciones IVA (303) ────────────────────────────────────
        [HttpGet("liquidaciones")]
        public async Task<ActionResult<IEnumerable<LiquidacionIVA>>> GetLiquidaciones([FromQuery] int? ejercicioId = null, [FromQuery] int? año = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.LiquidacionesIVA.Where(l => l.EmpresaId == empresaId);
            if (ejercicioId.HasValue) q = q.Where(l => l.EjercicioId == ejercicioId.Value);
            if (año.HasValue) q = q.Where(l => l.Año == año.Value);
            return Ok(await q.Include(l => l.Ejercicio).OrderByDescending(l => l.Año).ThenByDescending(l => l.Periodo).ToListAsync());
        }

        [HttpGet("liquidaciones/{id}")]
        public async Task<ActionResult<LiquidacionIVA>> GetLiquidacion(int id)
        {
            var l = await _context.LiquidacionesIVA.Include(x => x.Ejercicio).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return l == null ? NotFound() : Ok(l);
        }

        [HttpGet("liquidaciones/{id}/detalles")]
        public async Task<ActionResult<IEnumerable<DetalleLiquidacionIVA>>> GetDetalles(int id)
        {
            var liq = await _context.LiquidacionesIVA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (liq == null) return NotFound();
            return Ok(await _context.DetallesLiquidacionIVA.Where(d => d.LiquidacionIVAId == id).ToListAsync());
        }

        [HttpPost("liquidaciones/calcular")]
        public async Task<ActionResult<LiquidacionIVA>> Calcular([FromBody] CalcularDto dto)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            var ejercicio = await _context.EjerciciosContables.FirstOrDefaultAsync(e => e.Id == dto.EjercicioId && e.EmpresaId == empresaId);
            if (ejercicio == null) return BadRequest(new { Message = "Ejercicio no válido" });

            // Calcular periodo
            var (desde, hasta) = dto.Periodo switch
            {
                PeriodoIVA.PrimerTrimestre => (new DateTime(dto.Año, 1, 1), new DateTime(dto.Año, 3, 31)),
                PeriodoIVA.SegundoTrimestre => (new DateTime(dto.Año, 4, 1), new DateTime(dto.Año, 6, 30)),
                PeriodoIVA.TercerTrimestre => (new DateTime(dto.Año, 7, 1), new DateTime(dto.Año, 9, 30)),
                PeriodoIVA.CuartoTrimestre => (new DateTime(dto.Año, 10, 1), new DateTime(dto.Año, 12, 31)),
                _ => (new DateTime(dto.Año, 1, 1), new DateTime(dto.Año, 12, 31))
            };

            // Evitar duplicado
            if (await _context.LiquidacionesIVA.AnyAsync(l => l.EmpresaId == empresaId && l.EjercicioId == dto.EjercicioId && l.Periodo == dto.Periodo && l.Año == dto.Año))
                return Conflict(new { Message = "Ya existe liquidación para ese periodo" });

            // Obtener config IVA para prorrata
            var config = await _context.ConfiguracionesIVA.FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.EjercicioId == dto.EjercicioId);
            var prorrata = config?.AplicaProrrataGeneral == true ? (config.PorcentajeProrrataGeneralRedondeado ?? config.PorcentajeProrrataGeneral ?? 100m) : 100m;

            // Calcular bases desde documentos del periodo (simplificado: Documentos con IVA)
            var docs = await _context.Documentos.Include(d => d.Lineas).Where(d => d.EmpresaId == empresaId && d.Fecha >= desde && d.Fecha <= hasta && d.Estado != ERP.Domain.Entities.EstadoDocumento.Anulado).ToListAsync();

            var liq = new LiquidacionIVA
            {
                EmpresaId = empresaId, EjercicioId = dto.EjercicioId, Periodo = dto.Periodo, Año = dto.Año,
                FechaDesde = desde, FechaHasta = hasta, Estado = EstadoLiquidacionIVA.Calculada,
                PorcentajeProrrataAplicada = prorrata, UsaProrrataEspecial = config?.AplicaProrrataEspecial ?? false,
                UsaSectoresDiferenciados = config?.TieneSectoresDiferenciados ?? false,
                FechaCalculo = DateTime.Now, UsuarioCalculo = User.Identity?.Name
            };

            // Totales repercutido vs soportado (según EsCompra)
            foreach (var doc in docs)
            {
                var esCompra = doc.EsCompra;
                foreach (var linea in doc.Lineas)
                {
                    var tipo = linea.PorcentajeIva == 21 ? TipoIVA.General : linea.PorcentajeIva == 10 ? TipoIVA.Reducido : linea.PorcentajeIva == 4 ? TipoIVA.Superreducido : TipoIVA.ExentoArt20;
                    var baseImp = linea.Cantidad * linea.PrecioUnitario;
                    var cuota = baseImp * (linea.PorcentajeIva / 100m);
                    if (!esCompra)
                    {
                        if (tipo == TipoIVA.General) { liq.BaseGeneral += baseImp; liq.IVAGeneralRepercutido += cuota; }
                        else if (tipo == TipoIVA.Reducido) { liq.BaseReducida += baseImp; liq.IVAReducidoRepercutido += cuota; }
                        else if (tipo == TipoIVA.Superreducido) { liq.BaseSuperreducida += baseImp; liq.IVASuperreducidoRepercutido += cuota; }
                    }
                    else
                    {
                        var deducible = cuota * (prorrata / 100m);
                        if (tipo == TipoIVA.General) { liq.IVAGeneralSoportado += cuota; liq.IVADeducibleGeneral += deducible; }
                        else if (tipo == TipoIVA.Reducido) { liq.IVAReducidoSoportado += cuota; liq.IVADeducibleReducido += deducible; }
                        else if (tipo == TipoIVA.Superreducido) { liq.IVASuperreducidoSoportado += cuota; liq.IVADeducibleSuperreducido += deducible; }
                    }
                }
            }
            liq.IVADeducibleTotal = liq.IVADeducibleGeneral + liq.IVADeducibleReducido + liq.IVADeducibleSuperreducido;
            liq.IVADevengadoTotal = (liq.IVAGeneralRepercutido + liq.IVAReducidoRepercutido + liq.IVASuperreducidoRepercutido) - liq.IVADeducibleTotal;
            liq.IVAAIngresar = liq.IVADevengadoTotal > 0 ? liq.IVADevengadoTotal : 0;
            liq.IVADevolver = liq.IVADevengadoTotal < 0 ? -liq.IVADevengadoTotal : 0;

            _context.LiquidacionesIVA.Add(liq);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLiquidacion), new { id = liq.Id }, liq);
        }

        [HttpPost("liquidaciones/{id}/presentar")]
        public async Task<IActionResult> Presentar(int id, [FromBody] string referenciaAEAT)
        {
            var liq = await _context.LiquidacionesIVA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (liq == null) return NotFound();
            if (liq.Estado != EstadoLiquidacionIVA.Calculada && liq.Estado != EstadoLiquidacionIVA.Validada) return BadRequest(new { Message = "Solo calculada/validada se puede presentar" });
            liq.Estado = EstadoLiquidacionIVA.Presentada; liq.Presentado = true;
            liq.FechaPresentacion = DateTime.Now; liq.ReferenciaPresentacion = referenciaAEAT;
            liq.EsLiquidacionDefinitiva = true;
            await _context.SaveChangesAsync();
            return Ok(new { liq.Estado, liq.ReferenciaPresentacion });
        }

        // ── Libros registro IVA ─────────────────────────────────────────
        [HttpGet("libros-registro")]
        public async Task<ActionResult<IEnumerable<LibroRegistroIVA>>> GetLibros([FromQuery] int? ejercicioId = null, [FromQuery] TipoLibroIVA? tipo = null)
        {
            var q = _context.LibrosRegistroIVA.Where(l => l.EmpresaId == GetEmpresaId());
            if (ejercicioId.HasValue) q = q.Where(l => l.EjercicioId == ejercicioId.Value);
            if (tipo.HasValue) q = q.Where(l => l.TipoLibro == tipo.Value);
            return Ok(await q.OrderByDescending(l => l.FechaOperacion).ToListAsync());
        }

        public class CalcularDto { public int EjercicioId { get; set; } public PeriodoIVA Periodo { get; set; } public int Año { get; set; } }
    }
}
