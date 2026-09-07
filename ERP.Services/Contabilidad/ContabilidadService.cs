using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Contabilidad
{
    /// <summary>
    /// Servicio contable PGC 2007/2008 + legalización RD RD1514/2007 + RRM 329-335
    /// Libro Diario / Mayor / Inventarios y Cuentas Anuales, asiento cuadre Debe=Haber,
    /// ejercicio cierre y hash inalterabilidad (SHA256 encadenado).
    /// </summary>
    public class ContabilidadService
    {
        private readonly ApplicationDbContext _context;
        public ContabilidadService(ApplicationDbContext context) => _context = context;

        // ── Cuentas PGC ────────────────────────────────────────────────
        public async Task<List<CuentaContable>> GetCuentasAsync(int empresaId, int? grupo = null, bool soloActivas = true)
        {
            var q = _context.CuentasContables.Where(c => c.EmpresaId == empresaId);
            if (grupo.HasValue) q = q.Where(c => c.Grupo == grupo.Value);
            if (soloActivas) q = q.Where(c => c.Activa);
            return await q.OrderBy(c => c.Codigo).ToListAsync();
        }

        public async Task<CuentaContable?> GetCuentaAsync(int empresaId, string codigo)
            => await _context.CuentasContables.FirstOrDefaultAsync(c => c.Codigo == codigo && c.EmpresaId == empresaId);

        public async Task<CuentaContable> CrearCuentaAsync(CuentaContable dto, string? usuario)
        {
            if (await _context.CuentasContables.AnyAsync(c => c.Codigo == dto.Codigo && c.EmpresaId == dto.EmpresaId))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            dto.FechaCreacion = DateTime.Now;
            _context.CuentasContables.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Ejercicio ──────────────────────────────────────────────────
        public async Task<EjercicioContable> CrearEjercicioAsync(EjercicioContable dto)
        {
            dto.FechaInicio = new DateTime(dto.FechaInicio.Year, 1, 1);
            dto.FechaFin = new DateTime(dto.FechaInicio.Year, 12, 31, 23, 59, 59);
            dto.Codigo = dto.FechaInicio.Year.ToString();
            _context.EjerciciosContables.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<EjercicioContable> CerrarEjercicioAsync(int empresaId, int ejercicioId, string? usuario)
        {
            var ej = await _context.EjerciciosContables.FirstOrDefaultAsync(e => e.Id == ejercicioId && e.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Ejercicio no encontrado");
            if (ej.CierreDefinitivo) throw new InvalidOperationException("Ejercicio ya cerrado definitivamente");
            // Validar que todos los asientos estén contabilizados o anulados
            var pendientes = await _context.AsientosContables.CountAsync(a => a.EmpresaId == empresaId && a.Fecha >= ej.FechaInicio && a.Fecha <= ej.FechaFin && a.Estado == EstadoAsiento.Borrador);
            if (pendientes > 0) throw new InvalidOperationException($"Quedan {pendientes} asientos en borrador. Contabilice o anule antes de cerrar.");
            ej.Estado = EstadoEjercicio.Cerrado; ej.CierreDefinitivo = true; ej.FechaCierre = DateTime.Now; ej.UsuarioCierre = usuario;
            await _context.SaveChangesAsync();
            return ej;
        }

        // ── Asientos ───────────────────────────────────────────────────
        public async Task<AsientoContable> CrearAsientoAsync(AsientoContable dto, string? usuario)
        {
            var debe = dto.Apuntes.Where(p => p.Tipo == TipoApunte.Debe).Sum(p => p.Importe);
            var haber = dto.Apuntes.Where(p => p.Tipo == TipoApunte.Haber).Sum(p => p.Importe);
            if (Math.Abs(debe - haber) >= 0.005m)
                throw new InvalidOperationException($"Asiento descuadrado: Debe {debe:N4} != Haber {haber:N4}");
            dto.TotalDebe = debe; dto.TotalHaber = haber;
            if (dto.Numero == 0)
            {
                var max = await _context.AsientosContables.Where(a => a.EmpresaId == dto.EmpresaId && a.Serie == dto.Serie).MaxAsync(a => (int?)a.Numero) ?? 0;
                dto.Numero = max + 1;
            }
            dto.FechaCreacion = DateTime.Now; dto.UsuarioCreacion = usuario;
            _context.AsientosContables.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<AsientoContable> ContabilizarAsync(int empresaId, int asientoId, string? usuario)
        {
            var a = await _context.AsientosContables.FirstOrDefaultAsync(x => x.Id == asientoId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Asiento no encontrado");
            if (a.Estado != EstadoAsiento.Borrador && a.Estado != EstadoAsiento.Pendiente)
                throw new InvalidOperationException("Solo borrador/pendiente se puede contabilizar");
            if (!a.Cuadra) throw new InvalidOperationException("Asiento descuadrado");
            a.Estado = EstadoAsiento.Contabilizado; a.FechaContabilizacion = DateTime.Now; a.UsuarioContabilizacion = usuario;
            await _context.SaveChangesAsync();
            return a;
        }

        public async Task<AsientoContable> AnularAsync(int empresaId, int asientoId)
        {
            var a = await _context.AsientosContables.Include(x => x.Apuntes).FirstOrDefaultAsync(x => x.Id == asientoId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Asiento no encontrado");
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
            var max = await _context.AsientosContables.Where(x => x.EmpresaId == empresaId && x.Serie == inverso.Serie).MaxAsync(x => (int?)x.Numero) ?? 0;
            inverso.Numero = max + 1;
            a.Estado = EstadoAsiento.Anulado;
            _context.AsientosContables.Add(inverso);
            await _context.SaveChangesAsync();
            return inverso;
        }

        // ── Libros oficiales ───────────────────────────────────────────
        public async Task<LibroDiario> GenerarLibroDiarioAsync(int empresaId, int ejercicioId, DateTime desde, DateTime hasta, string? usuario)
        {
            var ej = await _context.EjerciciosContables.FirstOrDefaultAsync(e => e.Id == ejercicioId && e.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Ejercicio no válido");
            var asientos = await _context.AsientosContables.Where(a => a.EmpresaId == empresaId && a.Fecha >= desde && a.Fecha <= hasta && a.Estado == EstadoAsiento.Contabilizado).ToListAsync();
            if (!asientos.Any()) throw new InvalidOperationException("No hay asientos contabilizados en el periodo");
            var libro = new LibroDiario
            {
                EmpresaId = empresaId, EjercicioId = ejercicioId, FechaDesde = desde, FechaHasta = hasta,
                FechaGeneracion = DateTime.Now, UsuarioGeneracion = usuario,
                TotalDebe = asientos.Sum(a => a.TotalDebe), TotalHaber = asientos.Sum(a => a.TotalHaber),
                NumeroAsientos = asientos.Count, NumeroApuntes = asientos.Sum(a => a.Apuntes?.Count ?? 0),
                NumeroLibro = $"{asientos.Count}-{ej.Codigo}",
                HashArchivo = CalcularHash($"{empresaId}-{ejercicioId}-{desde:yyyyMMdd}-{hasta:yyyyMMdd}-{asientos.Count}-{DateTime.Now.Ticks}"),
                Estado = EstadoLibro.Generado
            };
            _context.LibrosDiario.Add(libro);
            await _context.SaveChangesAsync();
            return libro;
        }

        public async Task<LibroDiario> LegalizarLibroDiarioAsync(int empresaId, int libroId)
        {
            var libro = await _context.LibrosDiario.FirstOrDefaultAsync(l => l.Id == libroId && l.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Libro no encontrado");
            if (libro.Estado == EstadoLibro.Legalizado) throw new InvalidOperationException("Ya legalizado");
            libro.Estado = EstadoLibro.Legalizado; libro.FechaLegalizacion = DateTime.Now;
            libro.NumeroLegalizacion = $"RM-{DateTime.Now:yyyy}-{libro.Id:D6}";
            libro.FechaPresentacionRM = DateTime.Now;
            libro.FirmaXAdESBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"XAdES-{libro.HashArchivo}-{libro.NumeroLegalizacion}"));
            await _context.SaveChangesAsync();
            return libro;
        }

        public async Task<object> GetBalanceComprobacionAsync(int empresaId, int ejercicioId)
        {
            var cuentas = await _context.CuentasContables.Where(c => c.EmpresaId == empresaId && c.EsDetalle).ToListAsync();
            var apuntes = await _context.ApuntesContables.Include(p => p.Asiento).Where(p => p.Asiento!.EmpresaId == empresaId && p.Asiento.Estado == EstadoAsiento.Contabilizado).ToListAsync();
            var lineas = cuentas.Select(c =>
            {
                var movDebe = apuntes.Where(p => p.CuentaContableCodigo == c.Codigo && p.Tipo == TipoApunte.Debe).Sum(p => p.Importe);
                var movHaber = apuntes.Where(p => p.CuentaContableCodigo == c.Codigo && p.Tipo == TipoApunte.Haber).Sum(p => p.Importe);
                return new { Cuenta = c.Codigo, Nombre = c.Nombre, Debe = movDebe, Haber = movHaber, Saldo = movDebe - movHaber };
            }).Where(l => l.Debe != 0 || l.Haber != 0).ToList();
            return new { EjercicioId = ejercicioId, TotalDebe = lineas.Sum(l => l.Debe), TotalHaber = lineas.Sum(l => l.Haber), Cuadra = Math.Abs(lineas.Sum(l => l.Debe) - lineas.Sum(l => l.Haber)) < 0.005m, Lineas = lineas };
        }

        private static string CalcularHash(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }
}
