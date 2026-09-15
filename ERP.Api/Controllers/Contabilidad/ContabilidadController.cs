using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities.Contabilidad;
using ERP.Domain.DTOs.Contabilidad;
using ERP.Services.Contabilidad;
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
        private readonly ContabilidadService _conta;
        public ContabilidadController(ApplicationDbContext context, ContabilidadService conta) { _context = context; _conta = conta; }
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
        public async Task<ActionResult<CuentaContableDto>> PostCuenta(CuentaContableDto dto)
        {
            var empresaId = GetEmpresaId();
            var entity = new CuentaContable
            {
                Codigo = dto.Codigo, Nombre = dto.Nombre, Grupo = dto.Grupo, Nivel = dto.Nivel,
                CodigoPadre = dto.CodigoPadre, EsDetalle = dto.EsDetalle,
                Naturaleza = dto.Naturaleza == "Acreedora" ? NaturalezaCuenta.Acreedora : NaturalezaCuenta.Deudora,
                Descripcion = dto.Descripcion, Activa = dto.Activa, EmpresaId = empresaId
            };
            try
            {
                var creada = await _conta.CrearCuentaAsync(entity, User.Identity?.Name);
                return CreatedAtAction(nameof(GetCuenta), new { codigo = creada.Codigo }, new CuentaContableDto { Codigo = creada.Codigo, Nombre = creada.Nombre, Grupo = creada.Grupo });
            }
            catch (InvalidOperationException ex) { return Conflict(new { Message = ex.Message }); }
        }

        [HttpPost("cuentas/importar")]
        public async Task<ActionResult> ImportarPlan([FromBody] ImportarPlanContableDto dto)
        {
            dto.EmpresaId = GetEmpresaId();
            int creadas = 0;
            foreach (var c in dto.Cuentas)
            {
                if (await _context.CuentasContables.AnyAsync(x => x.Codigo == c.Codigo && x.EmpresaId == dto.EmpresaId) && !dto.SobrescribirExistentes) continue;
                var e = new CuentaContable { Codigo = c.Codigo, Nombre = c.Nombre, Grupo = c.Grupo, Nivel = c.Nivel, CodigoPadre = c.CodigoPadre, EsDetalle = c.EsDetalle, Naturaleza = c.Naturaleza == "Acreedora" ? NaturalezaCuenta.Acreedora : NaturalezaCuenta.Deudora, Descripcion = c.Descripcion, Activa = c.Activa, EmpresaId = dto.EmpresaId };
                _context.CuentasContables.Add(e); creadas++;
            }
            await _context.SaveChangesAsync();
            return Ok(new { Creadas = creadas });
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
        public async Task<ActionResult<AsientoContable>> PostAsiento(CrearAsientoDto dto)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return BadRequest();
            var entity = new AsientoContable
            {
                EmpresaId = empresaId, Serie = dto.Serie, Fecha = dto.Fecha, Concepto = dto.Concepto,
                Tipo = Enum.TryParse<TipoAsiento>(dto.TipoAsiento, true, out var t) ? t : TipoAsiento.Normal,
                OrigenTipo = dto.OrigenTipo, OrigenId = dto.OrigenId,
                Apuntes = dto.Apuntes.Select(a => new ApunteContable
                {
                    Orden = a.Orden, CuentaContableCodigo = a.CuentaContableCodigo,
                    Tipo = a.Tipo == "Haber" ? TipoApunte.Haber : TipoApunte.Debe,
                    Importe = a.Importe, Concepto = a.Concepto, CentroCosteId = a.CentroCosteId, ProyectoId = a.ProyectoId, DocumentoReferencia = a.DocumentoReferencia
                }).ToList()
            };
            try
            {
                var creada = await _conta.CrearAsientoAsync(entity, User.Identity?.Name);
                return CreatedAtAction(nameof(GetAsiento), new { id = creada.Id }, creada);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("asientos/{id}/contabilizar")]
        public async Task<IActionResult> Contabilizar(int id)
        {
            try { var a = await _conta.ContabilizarAsync(GetEmpresaId(), id, User.Identity?.Name); return Ok(new { a.Estado }); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("asientos/{id}/anular")]
        public async Task<IActionResult> Anular(int id)
        {
            try { var inv = await _conta.AnularAsync(GetEmpresaId(), id); return Ok(new { AnulacionId = inv.Id }); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
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
            try
            {
                var libro = await _conta.GenerarLibroDiarioAsync(GetEmpresaId(), ejercicioId, desde, hasta, User.Identity?.Name);
                return CreatedAtAction(nameof(GetLibrosDiario), new { id = libro.Id }, libro);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("libros/diario/{id}/legalizar")]
        public async Task<IActionResult> LegalizarDiario(int id)
        {
            try { var libro = await _conta.LegalizarLibroDiarioAsync(GetEmpresaId(), id); return Ok(new { libro.NumeroLegalizacion, libro.FechaLegalizacion }); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { Message = ex.Message }); }
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
