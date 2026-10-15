using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Contabilidad
{
    /// <summary>
    /// Activos fijos Fase A: alta + plan lineal + dotación con asiento 681→281 + baja.
    /// </summary>
    public class ActivoFijoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ContabilidadService _conta;

        public ActivoFijoService(ApplicationDbContext context, ContabilidadService conta)
        {
            _context = context;
            _conta = conta;
        }

        public async Task<List<ActivoFijo>> ListarAsync(int empresaId)
            => await _context.ActivosFijos
                .Include(a => a.PlanAmortizacion)
                .Where(a => a.EmpresaId == empresaId)
                .OrderBy(a => a.Codigo).ToListAsync();

        public async Task<ActivoFijo?> ObtenerAsync(int empresaId, int id)
            => await _context.ActivosFijos
                .Include(a => a.PlanAmortizacion.OrderBy(l => l.Ejercicio))
                .FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);

        public async Task<ActivoFijo> CrearAsync(ActivoFijo dto, string? usuario)
        {
            if (dto.PrecioAdquisicion <= 0) throw new ArgumentException("Precio debe ser > 0");
            if (dto.VidaUtilAnios <= 0) throw new ArgumentException("Vida útil debe ser >= 1");
            if (string.IsNullOrWhiteSpace(dto.Codigo))
                dto.Codigo = $"AF-{dto.FechaAdquisicion:yyyy}-{(await _context.ActivosFijos.CountAsync(a => a.EmpresaId == dto.EmpresaId) + 1):D4}";
            if (await _context.ActivosFijos.AnyAsync(a => a.EmpresaId == dto.EmpresaId && a.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            dto.UsuarioCreacion = usuario;
            dto.FechaCreacion = DateTime.Now;
            dto.Estado = EstadoActivoFijo.Activo;
            _context.ActivosFijos.Add(dto);
            await _context.SaveChangesAsync();
            await GenerarPlanAsync(dto.EmpresaId, dto.Id);
            return (await ObtenerAsync(dto.EmpresaId, dto.Id))!;
        }

        /// <summary>Genera plan lineal: dotación anual = (precio - residual) / vida. Ajusta última cuota por redondeo.</summary>
        public async Task<List<LineaAmortizacion>> GenerarPlanAsync(int empresaId, int activoId)
        {
            var activo = await _context.ActivosFijos
                .Include(a => a.PlanAmortizacion)
                .FirstOrDefaultAsync(a => a.Id == activoId && a.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Activo no encontrado");
            if (activo.PlanAmortizacion.Any(l => l.Contabilizado))
                throw new InvalidOperationException("No se puede regenerar: ya hay dotaciones contabilizadas");
            _context.LineasAmortizacion.RemoveRange(activo.PlanAmortizacion);
            var baseAmort = Math.Max(0, activo.PrecioAdquisicion - activo.ValorResidual);
            var anual = Math.Round(baseAmort / activo.VidaUtilAnios, 4);
            var acumulado = 0m;
            var inicio = activo.FechaPuestaMarcha.Year;
            var lineas = new List<LineaAmortizacion>();
            for (int i = 0; i < activo.VidaUtilAnios; i++)
            {
                var dot = (i == activo.VidaUtilAnios - 1) ? Math.Round(baseAmort - acumulado, 4) : anual;
                acumulado = Math.Round(acumulado + dot, 4);
                lineas.Add(new LineaAmortizacion
                {
                    ActivoFijoId = activo.Id,
                    Ejercicio = inicio + i,
                    FechaDotacion = new DateTime(inicio + i, 12, 31),
                    Dotacion = dot,
                    AmortizadoAcumulado = acumulado,
                    ValorNetoContable = Math.Round(activo.PrecioAdquisicion - acumulado, 4)
                });
            }
            _context.LineasAmortizacion.AddRange(lineas);
            await _context.SaveChangesAsync();
            return lineas;
        }

        /// <summary>Contabiliza la dotación de un ejercicio: Debe 681 / Haber 281.</summary>
        public async Task<AsientoContable> ContabilizarDotacionAsync(int empresaId, int activoId, int ejercicio, string? usuario)
        {
            var activo = await ObtenerAsync(empresaId, activoId) ?? throw new KeyNotFoundException("Activo no encontrado");
            if (activo.Estado == EstadoActivoFijo.Baja) throw new InvalidOperationException("Activo dado de baja");
            var linea = activo.PlanAmortizacion.FirstOrDefault(l => l.Ejercicio == ejercicio)
                ?? throw new KeyNotFoundException($"Sin línea para ejercicio {ejercicio}");
            if (linea.Contabilizado) throw new InvalidOperationException("Dotación ya contabilizada");
            var asiento = new AsientoContable
            {
                EmpresaId = empresaId,
                Serie = "AMO",
                Fecha = linea.FechaDotacion,
                Concepto = $"Dot. amortización {activo.Codigo} {activo.Descripcion} ej. {ejercicio}",
                Tipo = TipoAsiento.Amortizacion,
                Estado = EstadoAsiento.Borrador,
                OrigenTipo = "ActivoFijo",
                OrigenId = activo.Id,
                Apuntes = new List<ApunteContable>
                {
                    new() { Orden = 1, CuentaContableCodigo = activo.CuentaDotacionCodigo, Tipo = TipoApunte.Debe, Importe = linea.Dotacion, Concepto = $"Dotación {activo.Codigo}" },
                    new() { Orden = 2, CuentaContableCodigo = activo.CuentaAmortizacionCodigo, Tipo = TipoApunte.Haber, Importe = linea.Dotacion, Concepto = $"Amort. acum. {activo.Codigo}" }
                }
            };
            var creado = await _conta.CrearAsientoAsync(asiento, usuario);
            await _conta.ContabilizarAsync(empresaId, creado.Id, usuario);
            linea.Contabilizado = true;
            linea.AsientoContableId = creado.Id;
            // Si es la última línea → totalmente amortizado
            if (activo.PlanAmortizacion.All(l => l.Contabilizado || l.Id == linea.Id))
            {
                var todas = activo.PlanAmortizacion.Count;
                var contadas = activo.PlanAmortizacion.Count(l => l.Contabilizado) + 0; // linea aún no recargada
                if (todas <= 1 || activo.PlanAmortizacion.Where(l => l.Id != linea.Id).All(l => l.Contabilizado))
                    activo.Estado = EstadoActivoFijo.TotalmenteAmortizado;
            }
            await _context.SaveChangesAsync();
            return creado;
        }

        public async Task<ActivoFijo> DarDeBajaAsync(int empresaId, int activoId, DateTime fechaBaja, string motivo, string? usuario)
        {
            var activo = await ObtenerAsync(empresaId, activoId) ?? throw new KeyNotFoundException("Activo no encontrado");
            if (activo.Estado == EstadoActivoFijo.Baja) throw new InvalidOperationException("Ya está de baja");
            activo.Estado = EstadoActivoFijo.Baja;
            activo.FechaBaja = fechaBaja;
            activo.MotivoBaja = motivo;
            await _context.SaveChangesAsync();
            return activo;
        }
    }
}
