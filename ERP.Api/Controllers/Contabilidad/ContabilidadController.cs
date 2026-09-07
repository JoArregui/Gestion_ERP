using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities.Contabilidad;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Api.Controllers.Contabilidad
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContabilidadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ContabilidadController(ApplicationDbContext context) => _context = context;
        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Ejercicios ───────────────────────────────────────────────────
        [HttpGet("ejercicios")]
        public async Task<ActionResult<IEnumerable<EjercicioContable>>> GetEjercicios()
            => Ok(await _context.EjerciciosContables.Where(e => e.EmpresaId == GetEmpresaId()).OrderByDescending(e => e.FechaInicio).ToListAsync());

        [HttpGet("ejercicios/{id}")]
        public async Task<ActionResult<EjercicioContable>> GetEjercicio(int id)
        {
            var e = await _context.EjerciciosContables.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return e == null ? NotFound() : Ok(e);
        }

        [HttpPost("ejercicios")]
        public async Task<ActionResult<EjercicioContable>> PostEjercicio(EjercicioContable dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            _context.EjerciciosContables.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEjercicio), new { id = dto.Id }, dto);
        }

        [HttpPost("ejercicios/{id}/cerrar")]
        public async Task<IActionResult> CerrarEjercicio(int id)
        {
            var e = await _context.EjerciciosContables.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (e == null) return NotFound();
            if (e.CierreDefinitivo) return BadRequest(new { Message = "Ejercicio ya cerrado definitivamente" });
            e.Estado = EstadoEjercicio.Cerrado;
            e.CierreDefinitivo = true;
            e.FechaCierre = DateTime.Now;
            e.UsuarioCierre = User.Identity?.Name;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Ejercicio cerrado", e.Estado });
        }

        // ── Cuentas PGC ──────────────────────────────────────────────────
        [HttpGet("cuentas")]
        public async Task<ActionResult<IEnumerable<CuentaContable>>> GetCuentas([FromQuery] int? grupo = null, [FromQuery] bool? soloActivas = true)
        {
            var empresaId = GetEmpresaId();
            var q = _context.CuentasContables.Where(c => c.EmpresaId == empresaId);
            if (grupo.HasValue) q = q.Where(c => c.Grupo == grupo.Value);
            if (soloActivas == true) q = q.Where(c => c.Activa);
            return Ok(await q.OrderBy(c => c.Codigo).ToListAsync());
        }

        [HttpGet("cuentas/{codigo}")]
        public async Task<ActionResult<CuentaContable>> GetCuenta(string codigo)
        {
            var c = await _context.CuentasContables.FirstOrDefaultAsync(x => x.Codigo == codigo && x.EmpresaId == GetEmpresaId());
            return c == null ? NotFound() : Ok(c);
        }

        [HttpPost("cuentas")]
        public async Task<ActionResult<CuentaContable>> PostCuenta(CuentaContable dto)
        {
            dto.EmpresaId = GetEmpresaId();
            // Validar unicidad código por empresa
            if (await _context.CuentasContables.AnyAsync(x => x.Codigo == dto.Codigo && x.EmpresaId == dto.EmpresaId))
                return Conflict(new { Message = "Código ya existe" });
            dto.FechaCreacion = DateTime.Now;
            _context.CuentasContables.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCuenta), new { codigo = dto.Codigo }, dto);
        }

        [HttpPut("cuentas/{codigo}")]
        public async Task<IActionResult> PutCuenta(string codigo, CuentaContable dto)
        {
            if (codigo != dto.Codigo) return BadRequest();
            var existente = await _context.CuentasContables.FirstOrDefaultAsync(x => x.Codigo == codigo && x.EmpresaId == GetEmpresaId());
            if (existente == null) return NotFound();
            existente.Nombre = dto.Nombre; existente.Descripcion = dto.Descripcion; existente.Activa = dto.Activa;
            existente.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── Asientos + Apuntes ───────────────────────────────────────────
        [HttpGet("asientos")]
        public async Task<ActionResult<IEnumerable<AsientoContable>>> GetAsientos([FromQuery] int? ejercicioId = null, [FromQuery] EstadoAsiento? estado = null, [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.AsientosContables.Include(a => a.Apuntes).Where(a => a.EmpresaId == empresaId);
            if (ejercicioId.HasValue)
            {
                var ej = await _context.EjerciciosContables.FirstOrDefaultAsync(e => e.Id == ejercicioId.Value && e.EmpresaId == empresaId);
                if (ej != null) q = q.Where(a => a.Fecha >= ej.FechaInicio && a.Fecha <= ej.FechaFin);
            }
            if (estado.HasValue) q = q.Where(a => a.Estado == estado.Value);
            if (desde.HasValue) q = q.Where(a => a.Fecha >= desde.Value);
            if (hasta.HasValue) q = q.Where(a => a.Fecha <= hasta.Value);
            return Ok(await q.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Numero).ToListAsync());
        }

        [HttpGet("asientos/{id}")]
        public async Task<ActionResult<AsientoContable>> GetAsiento(int id)
        {
            var a = await _context.AsientosContables.Include(x => x.Apuntes).ThenInclude(p => p.CuentaContable).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return a == null ? NotFound() : Ok(a);
        }

        [HttpPost("asientos")]
        public async Task<ActionResult<AsientoContable>> PostAsiento(AsientoContable dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest();
            // Validar cuadre Debe=Haber
            var debe = dto.Apuntes.Where(p => p.Tipo == TipoApunte.Debe).Sum(p => p.Importe);
            var haber = dto.Apuntes.Where(p => p.Tipo == TipoApunte.Haber).Sum(p => p.Importe);
            if (Math.Abs(debe - haber) >= 0.005m) return BadRequest(new { Message = $"Asiento descuadrado: Debe {debe} != Haber {haber}" });
            dto.TotalDebe = debe; dto.TotalHaber = haber;
            // Autonumeración por Serie
            if (dto.Numero == 0)
            {
                var max = await _context.AsientosContables.Where(a => a.EmpresaId == dto.EmpresaId && a.Serie == dto.Serie).MaxAsync(a => (int?)a.Numero) ?? 0;
                dto.Numero = max + 1;
            }
            dto.FechaCreacion = DateTime.Now;
            dto.UsuarioCreacion = User.Identity?.Name;
            _context.AsientosContables.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAsiento), new { id = dto.Id }, dto);
        }

        [HttpPost("asientos/{id}/contabilizar")]
        public async Task<IActionResult> Contabilizar(int id)
        {
            var a = await _context.AsientosContables.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (a == null) return NotFound();
            if (a.Estado != EstadoAsiento.Borrador && a.Estado != EstadoAsiento.Pendiente) return BadRequest(new { Message = "Solo borrador/pendiente se puede contabilizar" });
            if (!a.Cuadra) return BadRequest(new { Message = "Asiento descuadrado" });
            a.Estado = EstadoAsiento.Contabilizado;
            a.FechaContabilizacion = DateTime.Now;
            a.UsuarioContabilizacion = User.Identity?.Name;
            await _context.SaveChangesAsync();
            return Ok(new { a.Estado });
        }

        [HttpPost("asientos/{id}/anular")]
        public async Task<IActionResult> Anular(int id)
        {
            var a = await _context.AsientosContables.Include(x => x.Apuntes).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (a == null) return NotFound();
            // Crear asiento inverso
            var inverso = new AsientoContable
            {
                EmpresaId = a.EmpresaId, Serie = a.Serie, Fecha = DateTime.Now,
                Concepto = $"Anulación {a.ReferenciaCompleta}: {a.Concepto}",
                Tipo = TipoAsiento.Ajuste, Estado = EstadoAsiento.Contabilizado,
                OrigenTipo = "Anulacion", OrigenId = a.Id,
                Apuntes = a.Apuntes.Select(p => new ApunteContable
                {
                    Orden = p.Orden, CuentaContableCodigo = p.CuentaContableCodigo,
                    Tipo = p.Tipo == TipoApunte.Debe ? TipoApunte.Haber : TipoApunte.Debe,
                    Importe = p.Importe, Concepto = p.Concepto
                }).ToList()
            };
            inverso.TotalDebe = inverso.Apuntes.Where(p => p.Tipo == TipoApunte.Debe).Sum(p => p.Importe);
            inverso.TotalHaber = inverso.Apuntes.Where(p => p.Tipo == TipoApunte.Haber).Sum(p => p.Importe);
            var max = await _context.AsientosContables.Where(x => x.EmpresaId == inverso.EmpresaId && x.Serie == inverso.Serie).MaxAsync(x => (int?)x.Numero) ?? 0;
            inverso.Numero = max + 1;
            a.Estado = EstadoAsiento.Anulado;
            _context.AsientosContables.Add(inverso);
            await _context.SaveChangesAsync();
            return Ok(new { AnulacionId = inverso.Id });
        }

        // ── Libros oficiales ────────────────────────────────────────────
        [HttpGet("libros/diario")]
        public async Task<ActionResult<IEnumerable<LibroDiario>>> GetLibrosDiario([FromQuery] int? ejercicioId = null)
        {
            var q = _context.LibrosDiario.Include(l => l.Ejercicio).Where(l => l.EmpresaId == GetEmpresaId());
            if (ejercicioId.HasValue) q = q.Where(l => l.EjercicioId == ejercicioId.Value);
            return Ok(await q.OrderByDescending(l => l.FechaGeneracion).ToListAsync());
        }

        [HttpPost("libros/diario/generar")]
        public async Task<ActionResult<LibroDiario>> GenerarLibroDiario([FromQuery] int ejercicioId, [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            var empresaId = GetEmpresaId();
            var ej = await _context.EjerciciosContables.FirstOrDefaultAsync(e => e.Id == ejercicioId && e.EmpresaId == empresaId);
            if (ej == null) return BadRequest(new { Message = "Ejercicio no válido" });
            var asientos = await _context.AsientosContables.Where(a => a.EmpresaId == empresaId && a.Fecha >= desde && a.Fecha <= hasta && a.Estado == EstadoAsiento.Contabilizado).ToListAsync();
            if (!asientos.Any()) return BadRequest(new { Message = "No hay asientos contabilizados en el periodo" });
            var libro = new LibroDiario
            {
                EmpresaId = empresaId, EjercicioId = ejercicioId, FechaDesde = desde, FechaHasta = hasta,
                FechaGeneracion = DateTime.Now, UsuarioGeneracion = User.Identity?.Name,
                TotalDebe = asientos.Sum(a => a.TotalDebe), TotalHaber = asientos.Sum(a => a.TotalHaber),
                NumeroAsientos = asientos.Count, NumeroApuntes = asientos.Sum(a => a.NumeroApuntes),
                NumeroLibro = $"{asientos.Count}-{ej.Codigo}",
                HashArchivo = CalcularHash($"{empresaId}-{ejercicioId}-{desde:yyyyMMdd}-{hasta:yyyyMMdd}-{asientos.Count}"),
                Estado = EstadoLibro.Generado
            };
            _context.LibrosDiario.Add(libro);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLibrosDiario), new { id = libro.Id }, libro);
        }

        [HttpPost("libros/diario/{id}/legalizar")]
        public async Task<IActionResult> LegalizarDiario(int id)
        {
            var libro = await _context.LibrosDiario.FirstOrDefaultAsync(l => l.Id == id && l.EmpresaId == GetEmpresaId());
            if (libro == null) return NotFound();
            if (libro.Estado == EstadoLibro.Legalizado) return BadRequest(new { Message = "Ya legalizado" });
            libro.Estado = EstadoLibro.Legalizado; libro.FechaLegalizacion = DateTime.Now;
            libro.NumeroLegalizacion = $"RM-{DateTime.Now:yyyy}-{libro.Id:D6}";
            libro.FechaPresentacionRM = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { libro.NumeroLegalizacion, libro.FechaLegalizacion });
        }

        [HttpGet("libros/mayor/{cuentaCodigo}")]
        public async Task<ActionResult> GetLibroMayor(string cuentaCodigo, [FromQuery] int ejercicioId)
        {
            var empresaId = GetEmpresaId();
            var apuntes = await _context.ApuntesContables
                .Include(p => p.Asiento)
                .Where(p => p.CuentaContableCodigo == cuentaCodigo && p.Asiento!.EmpresaId == empresaId)
                .ToListAsync();
            var debe = apuntes.Where(p => p.Tipo == TipoApunte.Debe).Sum(p => p.Importe);
            var haber = apuntes.Where(p => p.Tipo == TipoApunte.Haber).Sum(p => p.Importe);
            return Ok(new { Cuenta = cuentaCodigo, Debe = debe, Haber = haber, Saldo = debe - haber, Apuntes = apuntes.Count });
        }

        private static string CalcularHash(string input)
        {
            using var sha = SHA256.Create();
            var b = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(b);
        }
    }
}
