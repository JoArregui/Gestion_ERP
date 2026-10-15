using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.DTOs;
using ERP.Domain.DTOs.Almacen;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Almacen;
using EntAlm = ERP.Domain.Entities.Almacen.Almacen;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Almacen
{
    /// <summary>
    /// WMS mínimo (Fase D flujo ERP.txt §4): multialmacén, ubicaciones, picking y reposición.
    /// Articulo.Stock sigue siendo el total físico; las ubicaciones lo desglosan.
    /// </summary>
    public class AlmacenService
    {
        private readonly ApplicationDbContext _context;

        public AlmacenService(ApplicationDbContext context) => _context = context;

        // ── Maestros ───────────────────────────────────────────
        public async Task<List<EntAlm>> ListarAlmacenesAsync(int empresaId)
            => await _context.Almacenes.Where(a => a.EmpresaId == empresaId).OrderBy(a => a.Codigo).ToListAsync();

        public async Task<EntAlm> CrearAlmacenAsync(EntAlm dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) throw new ArgumentException("Nombre obligatorio");
            if (string.IsNullOrWhiteSpace(dto.Codigo))
            {
                var n = await _context.Almacenes.CountAsync(a => a.EmpresaId == dto.EmpresaId);
                dto.Codigo = $"ALM-{(n + 1):D2}";
            }
            if (await _context.Almacenes.AnyAsync(a => a.EmpresaId == dto.EmpresaId && a.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            if (dto.EsPrincipal)
                foreach (var otro in await _context.Almacenes.Where(a => a.EmpresaId == dto.EmpresaId && a.EsPrincipal).ToListAsync())
                    otro.EsPrincipal = false;
            else if (!await _context.Almacenes.AnyAsync(a => a.EmpresaId == dto.EmpresaId))
                dto.EsPrincipal = true; // el primero siempre es principal
            _context.Almacenes.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<List<Ubicacion>> ListarUbicacionesAsync(int empresaId, int almacenId)
        {
            if (!await _context.Almacenes.AnyAsync(a => a.Id == almacenId && a.EmpresaId == empresaId))
                throw new KeyNotFoundException("Almacén no encontrado");
            return await _context.Ubicaciones.Where(u => u.AlmacenId == almacenId).OrderBy(u => u.Pasillo).ThenBy(u => u.Estanteria).ThenBy(u => u.Nivel).ToListAsync();
        }

        public async Task<Ubicacion> CrearUbicacionAsync(int empresaId, Ubicacion dto)
        {
            if (!await _context.Almacenes.AnyAsync(a => a.Id == dto.AlmacenId && a.EmpresaId == empresaId))
                throw new KeyNotFoundException("Almacén no encontrado");
            if (string.IsNullOrWhiteSpace(dto.Codigo))
                dto.Codigo = $"{dto.Pasillo}-{dto.Estanteria}-{dto.Nivel}";
            if (await _context.Ubicaciones.AnyAsync(u => u.AlmacenId == dto.AlmacenId && u.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Ubicación {dto.Codigo} ya existe en el almacén");
            _context.Ubicaciones.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Movimientos entre ubicaciones ──────────────────────
        private async Task<Ubicacion> UbicacionDeEmpresaAsync(int empresaId, int ubicacionId)
        {
            var u = await _context.Ubicaciones.Include(x => x.Almacen).FirstOrDefaultAsync(x => x.Id == ubicacionId)
                ?? throw new KeyNotFoundException("Ubicación no encontrada");
            if (u.Almacen == null || u.Almacen.EmpresaId != empresaId || !u.Activa)
                throw new InvalidOperationException("Ubicación no válida para la empresa");
            return u;
        }

        private async Task<Articulo> ArticuloDeEmpresaAsync(int empresaId, int articuloId)
            => await _context.Articulos.FirstOrDefaultAsync(a => a.Id == articuloId && a.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Artículo no encontrado");

        public async Task<decimal> SinUbicarAsync(int empresaId, int articuloId)
        {
            var art = await ArticuloDeEmpresaAsync(empresaId, articuloId);
            var ubicado = await _context.StockUbicaciones.Where(s => s.ArticuloId == articuloId).SumAsync(s => (decimal?)s.Cantidad) ?? 0;
            return art.Stock - art.StockReservado - ubicado;
        }

        public async Task<StockUbicacion> UbicarStockAsync(int empresaId, int articuloId, int ubicacionId, decimal cantidad)
        {
            if (cantidad <= 0) throw new ArgumentException("Cantidad debe ser positiva");
            await ArticuloDeEmpresaAsync(empresaId, articuloId);
            await UbicacionDeEmpresaAsync(empresaId, ubicacionId);
            if (await SinUbicarAsync(empresaId, articuloId) < cantidad)
                throw new InvalidOperationException("Sin stock libre suficiente (descontando reservado y ubicado)");

            var row = await _context.StockUbicaciones.FirstOrDefaultAsync(s => s.UbicacionId == ubicacionId && s.ArticuloId == articuloId);
            if (row == null)
            {
                var ub = await _context.Ubicaciones.FindAsync(ubicacionId);
                row = new StockUbicacion { EmpresaId = empresaId, AlmacenId = ub!.AlmacenId, UbicacionId = ubicacionId, ArticuloId = articuloId };
                _context.StockUbicaciones.Add(row);
            }
            row.Cantidad += cantidad;
            row.UltimoMovimiento = DateTime.Now;
            await _context.SaveChangesAsync();
            return row;
        }

        /// <summary>Mueve entre ubicaciones; destino null = devolver a "sin ubicar".</summary>
        public async Task MoverStockAsync(int empresaId, int articuloId, int origenUbicacionId, int? destinoUbicacionId, decimal cantidad)
        {
            if (cantidad <= 0) throw new ArgumentException("Cantidad debe ser positiva");
            if (destinoUbicacionId == origenUbicacionId) throw new ArgumentException("Origen y destino iguales");
            await ArticuloDeEmpresaAsync(empresaId, articuloId);
            await UbicacionDeEmpresaAsync(empresaId, origenUbicacionId);
            if (destinoUbicacionId.HasValue) await UbicacionDeEmpresaAsync(empresaId, destinoUbicacionId.Value);

            var origen = await _context.StockUbicaciones.FirstOrDefaultAsync(s => s.UbicacionId == origenUbicacionId && s.ArticuloId == articuloId)
                ?? throw new InvalidOperationException("Sin existencias en la ubicación origen");
            if (origen.Cantidad - origen.Reservado < cantidad)
                throw new InvalidOperationException("La ubicación origen no tiene disponible suficiente (reservas)");

            origen.Cantidad -= cantidad;
            origen.UltimoMovimiento = DateTime.Now;
            if (destinoUbicacionId.HasValue)
            {
                var ub = await _context.Ubicaciones.FindAsync(destinoUbicacionId.Value);
                var dest = await _context.StockUbicaciones.FirstOrDefaultAsync(s => s.UbicacionId == destinoUbicacionId.Value && s.ArticuloId == articuloId);
                if (dest == null)
                {
                    dest = new StockUbicacion { EmpresaId = empresaId, AlmacenId = ub!.AlmacenId, UbicacionId = destinoUbicacionId.Value, ArticuloId = articuloId };
                    _context.StockUbicaciones.Add(dest);
                }
                dest.Cantidad += cantidad;
                dest.UltimoMovimiento = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<StockDetallado> GetDetalleAsync(int empresaId, int articuloId)
        {
            var art = await ArticuloDeEmpresaAsync(empresaId, articuloId);
            var rows = await _context.StockUbicaciones.Include(s => s.Ubicacion).ThenInclude(u => u!.Almacen)
                .Where(s => s.ArticuloId == articuloId).ToListAsync();
            var huecos = rows.Select(s => new HuecoStock(s.UbicacionId, s.Ubicacion?.Almacen?.Codigo ?? "?",
                s.Ubicacion?.Codigo ?? "?", s.Cantidad, s.Reservado, s.Cantidad - s.Reservado)).ToList();
            var ubicado = rows.Sum(s => s.Cantidad);
            return new StockDetallado(art.Id, art.Codigo, art.Descripcion, art.Stock, art.StockReservado,
                ubicado, art.Stock - art.StockReservado - ubicado, huecos);
        }

        // ── Reserva / consumo por ruta (llamado desde StockService) ──
        private IOrderedQueryable<StockUbicacion> Ruta(IQueryable<StockUbicacion> q)
            => q.Include(s => s.Ubicacion).ThenInclude(u => u!.Almacen)
                .OrderBy(s => s.Ubicacion!.Almacen!.Codigo).ThenBy(s => s.Ubicacion!.Pasillo).ThenBy(s => s.Ubicacion!.Estanteria).ThenBy(s => s.Ubicacion!.Nivel);

        public async Task ReservarEnUbicacionesAsync(int empresaId, int articuloId, decimal cantidad)
        {
            if (cantidad <= 0) return;
            var rows = await Ruta(_context.StockUbicaciones.Where(s => s.ArticuloId == articuloId && s.Cantidad > s.Reservado)).ToListAsync();
            // Solo ubicaciones de la empresa (vía almacén)
            rows = rows.Where(r => r.Ubicacion?.Almacen?.EmpresaId == empresaId).ToList();
            var resto = cantidad;
            foreach (var r in rows)
            {
                if (resto <= 0) break;
                var disp = r.Cantidad - r.Reservado;
                var toma = Math.Min(disp, resto);
                r.Reservado += toma;
                resto -= toma;
            }
            await _context.SaveChangesAsync();
        }

        public async Task LiberarReservaAsync(int empresaId, int articuloId, decimal cantidad)
        {
            if (cantidad <= 0) return;
            var rows = await Ruta(_context.StockUbicaciones.Where(s => s.ArticuloId == articuloId && s.Reservado > 0)).ToListAsync();
            rows = rows.Where(r => r.Ubicacion?.Almacen?.EmpresaId == empresaId).ToList();
            var resto = cantidad;
            foreach (var r in rows)
            {
                if (resto <= 0) break;
                var toma = Math.Min(r.Reservado, resto);
                r.Reservado -= toma;
                resto -= toma;
            }
            await _context.SaveChangesAsync();
        }

        public async Task ConsumirEnUbicacionesAsync(int empresaId, int articuloId, decimal cantidad)
        {
            if (cantidad <= 0) return;
            var rows = await Ruta(_context.StockUbicaciones.Where(s => s.ArticuloId == articuloId && s.Cantidad > 0)).ToListAsync();
            rows = rows.Where(r => r.Ubicacion?.Almacen?.EmpresaId == empresaId).ToList();
            var resto = cantidad;
            foreach (var r in rows)
            {
                if (resto <= 0) break;
                // Primero lo reservado, luego el resto
                var tomaRes = Math.Min(r.Reservado, resto);
                r.Reservado -= tomaRes;
                r.Cantidad -= tomaRes;
                resto -= tomaRes;
                if (resto <= 0) break;
                var toma = Math.Min(r.Cantidad, resto);
                r.Cantidad -= toma;
                resto -= toma;
                r.UltimoMovimiento = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        // ── Picking (lista de recogida en orden de ruta) ──────
        public async Task<PickingDocumento> GenerarPickingAsync(int empresaId, int documentoId)
        {
            var doc = await _context.Documentos.Include(d => d.Lineas).ThenInclude(l => l.Articulo)
                .FirstOrDefaultAsync(d => d.Id == documentoId && d.EmpresaId == empresaId && !d.EsCompra)
                ?? throw new KeyNotFoundException("Documento de venta no encontrado");
            var lineas = new List<LineaPicking>();
            var completo = true;
            foreach (var l in doc.Lineas)
            {
                if (l.ArticuloId == null) continue;
                var rows = await Ruta(_context.StockUbicaciones.Where(s => s.ArticuloId == l.ArticuloId.Value && s.Cantidad > s.Reservado)).ToListAsync();
                rows = rows.Where(r => r.Ubicacion?.Almacen?.EmpresaId == empresaId).ToList();
                var resto = l.Cantidad;
                var origen = new List<HuecoPicking>();
                foreach (var r in rows)
                {
                    if (resto <= 0) break;
                    var toma = Math.Min(r.Cantidad - r.Reservado, resto);
                    origen.Add(new HuecoPicking(r.UbicacionId, $"{r.Ubicacion!.Almacen!.Codigo}/{r.Ubicacion!.Codigo}", toma));
                    resto -= toma;
                }
                if (resto > 0)
                {
                    // Resto desde "sin ubicar" (playa/muelle)
                    var art = l.Articulo ?? await _context.Articulos.FindAsync(l.ArticuloId.Value);
                    var ubicado = await _context.StockUbicaciones.Where(s => s.ArticuloId == l.ArticuloId.Value).SumAsync(s => (decimal?)s.Cantidad) ?? 0;
                    var libre = (art?.Stock ?? 0) - (art?.StockReservado ?? 0) - ubicado;
                    var toma = Math.Min(Math.Max(0, libre), resto);
                    if (toma > 0) origen.Add(new HuecoPicking(null, "SIN-UBICAR", toma));
                    resto -= toma;
                }
                if (resto > 0.0001m) completo = false;
                lineas.Add(new LineaPicking(l.ArticuloId.Value, l.Articulo?.Codigo ?? "?", l.DescripcionArticulo, l.Cantidad, origen));
            }
            return new PickingDocumento(doc.Id, doc.NumeroDocumento, lineas, completo);
        }

        // ── Reposición automática (D2) ─────────────────────────
        public async Task<List<AlertaStockDTO>> GetReposicionAsync(int empresaId)
        {
            var arts = await _context.Articulos.Include(a => a.ProveedorHabitual)
                .Where(a => a.EmpresaId == empresaId && !a.IsDescatalogado && a.StockMinimo > 0
                    && (a.Stock - a.StockReservado) <= a.StockMinimo)
                .ToListAsync();
            return arts.Select(a => new AlertaStockDTO
            {
                ArticuloId = a.Id,
                Codigo = a.Codigo,
                Descripcion = a.Descripcion,
                StockActual = a.Stock - a.StockReservado,
                StockMinimo = a.StockMinimo,
                ProveedorId = a.ProveedorHabitualId ?? 0,
                ProveedorNombre = a.ProveedorHabitual?.RazonSocial ?? "Sin Proveedor"
            }).ToList();
        }
    }
}
