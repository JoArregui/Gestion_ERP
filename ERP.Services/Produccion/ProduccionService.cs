using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Produccion;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Produccion
{
    /// <summary>
    /// Producción ligera Fase E: BOM, centros, hojas ruta, Órdenes Fabricación.
    /// Explosión BOM → OFMateriales, coste teórico, consumo real, imputación horas.
    /// </summary>
    public class ProduccionService
    {
        private readonly ApplicationDbContext _context;

        public ProduccionService(ApplicationDbContext context) => _context = context;

        // ── BOM ──────────────────────────────────────────────────────
        public async Task<List<BOM>> GetBOMAsync(int empresaId, int articuloPadreId)
            => await _context.BOMLineas
                .Include(b => b.Componente)
                .Include(b => b.SustitutoComponente)
                .Where(b => b.EmpresaId == empresaId && b.ArticuloPadreId == articuloPadreId && b.Activa)
                .OrderBy(b => b.Componente!.Codigo)
                .ToListAsync();

        public async Task<BOM> AddComponenteAsync(BOM dto)
        {
            if (dto.Cantidad <= 0) throw new ArgumentException("Cantidad debe ser > 0");
            if (!await _context.Articulos.AnyAsync(a => a.Id == dto.ArticuloPadreId && a.EmpresaId == dto.EmpresaId))
                throw new ArgumentException("Artículo padre no existe");
            if (!await _context.Articulos.AnyAsync(a => a.Id == dto.ComponenteId && a.EmpresaId == dto.EmpresaId))
                throw new ArgumentException("Componente no existe");
            if (await _context.BOMLineas.AnyAsync(b => b.EmpresaId == dto.EmpresaId && b.ArticuloPadreId == dto.ArticuloPadreId && b.ComponenteId == dto.ComponenteId && b.Activa && b.Version == dto.Version))
                throw new InvalidOperationException("Componente ya existe en esta BOM versión");
            _context.BOMLineas.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Centros de Trabajo ──────────────────────────────────────
        public async Task<List<CentroTrabajo>> GetCentrosAsync(int empresaId)
            => await _context.CentrosTrabajo.Where(c => c.EmpresaId == empresaId && c.Activo).OrderBy(c => c.Codigo).ToListAsync();

        public async Task<CentroTrabajo> CrearCentroAsync(CentroTrabajo dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo))
            {
                var n = await _context.CentrosTrabajo.CountAsync(c => c.EmpresaId == dto.EmpresaId);
                dto.Codigo = $"CT-{(n + 1):D2}";
            }
            if (await _context.CentrosTrabajo.AnyAsync(c => c.EmpresaId == dto.EmpresaId && c.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            _context.CentrosTrabajo.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Hoja de Ruta ────────────────────────────────────────────
        public async Task<List<HojaRuta>> GetHojaRutaAsync(int empresaId, int articuloId)
            => await _context.HojasRuta
                .Include(h => h.CentroTrabajo)
                .Where(h => h.EmpresaId == empresaId && h.ArticuloId == articuloId && h.Activa)
                .OrderBy(h => h.NumeroOperacion)
                .ToListAsync();

        public async Task<HojaRuta> AddOperacionAsync(HojaRuta dto)
        {
            if (!await _context.Articulos.AnyAsync(a => a.Id == dto.ArticuloId && a.EmpresaId == dto.EmpresaId))
                throw new ArgumentException("Artículo no existe");
            if (!await _context.CentrosTrabajo.AnyAsync(c => c.Id == dto.CentroTrabajoId && c.EmpresaId == dto.EmpresaId))
                throw new ArgumentException("Centro de trabajo no existe");
            if (await _context.HojasRuta.AnyAsync(h => h.EmpresaId == dto.EmpresaId && h.ArticuloId == dto.ArticuloId && h.NumeroOperacion == dto.NumeroOperacion && h.Version == dto.Version && h.Activa))
                throw new InvalidOperationException("Operación ya existe en esta versión");
            _context.HojasRuta.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Órdenes de Fabricación ──────────────────────────────────
        public async Task<List<OrdenFabricacion>> ListarOFAsync(int empresaId, EstadoOF? estado = null)
        {
            var q = _context.OrdenesFabricacion
                .Include(o => o.Articulo)
                .Include(o => o.Materiales).ThenInclude(m => m.Articulo)
                .Include(o => o.Operaciones).ThenInclude(op => op.CentroTrabajo)
                .Where(o => o.EmpresaId == empresaId);
            if (estado.HasValue) q = q.Where(o => o.Estado == estado.Value);
            return await q.OrderByDescending(o => o.FechaCreacion).ToListAsync();
        }

        public async Task<OrdenFabricacion?> GetOFAsync(int empresaId, int id)
            => await _context.OrdenesFabricacion
                .Include(o => o.Articulo)
                .Include(o => o.Materiales).ThenInclude(m => m.Articulo)
                .Include(o => o.Operaciones).ThenInclude(op => op.CentroTrabajo)
                .FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);

        /// <summary>
        /// Crea OF desde BOM: explota materiales y operaciones de hoja ruta.
        /// </summary>
        public async Task<OrdenFabricacion> CrearOFAsync(OrdenFabricacion dto)
        {
            if (dto.CantidadPlanificada <= 0) throw new ArgumentException("Cantidad planificada debe ser > 0");
            if (string.IsNullOrWhiteSpace(dto.NumeroOF))
            {
                var n = await _context.OrdenesFabricacion.CountAsync(o => o.EmpresaId == dto.EmpresaId);
                dto.NumeroOF = $"OF-{{dto.FechaCreacion.Year:D4}}-{{(n + 1):D4}}";
            }
            if (await _context.OrdenesFabricacion.AnyAsync(o => o.EmpresaId == dto.EmpresaId && o.NumeroOF == dto.NumeroOF))
                throw new InvalidOperationException($"Número OF {dto.NumeroOF} ya existe");

            // Explosión BOM → materiales
            var bom = await GetBOMAsync(dto.EmpresaId, dto.ArticuloId);
            if (!bom.Any()) throw new InvalidOperationException("El artículo no tiene BOM definida");
            foreach (var b in bom)
            {
                dto.Materiales.Add(new OFMaterial
                {
                    ArticuloId = b.ComponenteId,
                    CantidadTeorica = b.Cantidad * dto.CantidadPlanificada,
                    CantidadReal = 0
                });
            }

            // Copia hoja ruta → operaciones
            var hr = await GetHojaRutaAsync(dto.EmpresaId, dto.ArticuloId);
            foreach (var h in hr)
            {
                dto.Operaciones.Add(new OFOperacion
                {
                    HojaRutaId = h.Id,
                    CentroTrabajoId = h.CentroTrabajoId,
                    NumeroOperacion = h.NumeroOperacion,
                    TiempoPreparacionRealMin = h.TiempoPreparacionMin,
                    TiempoUnidadRealMin = h.TiempoPorUnidadMin
                });
            }

            dto.Estado = EstadoOF.Planificada;
            dto.CosteMaterialTeorico = dto.Materiales.Sum(m => m.CantidadTeorica * _context.Articulos.Find(m.ArticuloId)?.PrecioCompra ?? 0);
            _context.OrdenesFabricacion.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        /// <summary>Inicia la OF: consume materiales (reserva stock), marca EnProceso.</summary>
        public async Task<OrdenFabricacion> IniciarOFAsync(int empresaId, int ofId)
        {
            var of = await GetOFAsync(empresaId, ofId) ?? throw new KeyNotFoundException("OF no encontrada");
            if (of.Estado != EstadoOF.Planificada) throw new InvalidOperationException("Solo Planificada se puede iniciar");

            // Reservar stock de materiales
            foreach (var m in of.Materiales)
            {
                var art = await _context.Articulos.FindAsync(m.ArticuloId);
                if (art == null) throw new KeyNotFoundException($"Material {m.ArticuloId} no encontrado");
                if (art.Stock < m.CantidadTeorica) throw new InvalidOperationException($"Stock insuficiente para {art.Codigo}: {art.Stock} < {m.CantidadTeorica}");
                art.StockReservado += m.CantidadTeorica;
                m.CantidadReal = m.CantidadTeorica; // asumimos consumo teórico al iniciar
            }
            of.Estado = EstadoOF.EnProceso;
            of.FechaRealInicio = DateTime.Now;
            of.CosteMaterialReal = of.Materiales.Sum(m => m.CantidadReal * (_context.Articulos.Find(m.ArticuloId)?.PrecioCompra ?? 0));
            await _context.SaveChangesAsync();
            return of;
        }

        /// <summary>Imputa horas en una operación: calcula coste MO + máquina y avanza unidades.</summary>
        public async Task<OFOperacion> ImputarHorasAsync(int empresaId, int ofId, int operacionId, decimal tiempoPrepMin, decimal tiempoUnidadMin, decimal unidades, string? operarioId)
        {
            var of = await GetOFAsync(empresaId, ofId) ?? throw new KeyNotFoundException("OF no encontrada");
            var op = of.Operaciones.FirstOrDefault(o => o.Id == operacionId) ?? throw new KeyNotFoundException("Operación no encontrada");
            if (of.Estado != EstadoOF.EnProceso) throw new InvalidOperationException("OF no está en proceso");

            op.TiempoPreparacionRealMin = tiempoPrepMin;
            op.TiempoUnidadRealMin = tiempoUnidadMin;
            op.UnidadesProducidas = unidades;
            op.FechaFin = DateTime.Now;
            op.OperarioId = operarioId;

            // Costes: MO = (prep + unidad * uds) * costeHoraMO / 60; Máquina = (prep + unidad * uds) * costeHoraMaq / 60
            var ct = op.CentroTrabajo ?? await _context.CentrosTrabajo.FindAsync(op.CentroTrabajoId);
            if (ct != null)
            {
                var totalMin = tiempoPrepMin + tiempoUnidadMin * unidades;
                op.CosteManoObra = Math.Round(totalMin * (ct.CosteHoraManoObra / 60m), 4);
                op.CosteMaquina = Math.Round(totalMin * (ct.CosteHoraMaquina / 60m), 4);
            }
            of.CosteManoObraReal = of.Operaciones.Sum(o => o.CosteManoObra);
            of.CosteMaquinaReal = of.Operaciones.Sum(o => o.CosteMaquina);
            await _context.SaveChangesAsync();
            return op;
        }

        /// <summary>Termina OF: consume stock reservado, da entrada a terminado, cierra coste.</summary>
        public async Task<OrdenFabricacion> TerminarOFAsync(int empresaId, int ofId, decimal cantidadBuena, decimal cantidadRechazada = 0)
        {
            var of = await GetOFAsync(empresaId, ofId) ?? throw new KeyNotFoundException("OF no encontrada");
            if (of.Estado != EstadoOF.EnProceso) throw new InvalidOperationException("Solo EnProceso se puede terminar");
            if (cantidadBuena < 0 || cantidadRechazada < 0) throw new ArgumentException("Cantidades no negativas");

            // Liberar reserva y consumir materiales
            foreach (var m in of.Materiales)
            {
                var art = await _context.Articulos.FindAsync(m.ArticuloId);
                if (art != null)
                {
                    art.StockReservado = Math.Max(0, art.StockReservado - m.CantidadReal);
                    art.Stock -= m.CantidadReal;
                }
            }

            // Entrada producto terminado
            var terminado = await _context.Articulos.FindAsync(of.ArticuloId);
            if (terminado != null)
            {
                terminado.Stock += cantidadBuena;
            }

            of.CantidadTerminada = cantidadBuena;
            of.CantidadRechazada = cantidadRechazada;
            of.Estado = EstadoOF.Terminada;
            of.FechaRealFin = DateTime.Now;
            of.CosteMaterialReal = of.Materiales.Sum(m => m.CantidadReal * (_context.Articulos.Find(m.ArticuloId)?.PrecioCompra ?? 0));
            of.CosteIndirectoReal = Math.Round((of.CosteManoObraReal + of.CosteMaquinaReal) * 0.15m, 4); // 15% indirectos ejemplo
            await _context.SaveChangesAsync();
            return of;
        }

        /// <summary>Coste unitario real de la OF (material + MO + máquina + indirectos) / unidades buenas.</summary>
        public async Task<decimal> GetCosteUnitarioAsync(int empresaId, int ofId)
        {
            var of = await GetOFAsync(empresaId, ofId) ?? throw new KeyNotFoundException("OF no encontrada");
            if (of.CantidadTerminada <= 0) return 0;
            var total = of.CosteMaterialReal + of.CosteManoObraReal + of.CosteMaquinaReal + of.CosteIndirectoReal;
            return Math.Round(total / of.CantidadTerminada, 4);
        }
    }
}