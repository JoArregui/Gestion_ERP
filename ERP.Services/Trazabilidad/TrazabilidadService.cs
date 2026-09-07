using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities.Trazabilidad;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Trazabilidad
{
    /// <summary>
    /// Servicio trazabilidad Reg. CE 178/2002 Art.18 — un paso atrás / un paso adelante
    /// Lote, movimientos con stock, alertas caducidad/APPCC, retiradas RASFF/AESAN.
    /// </summary>
    public class TrazabilidadService
    {
        private readonly ApplicationDbContext _context;
        public TrazabilidadService(ApplicationDbContext context) => _context = context;

        // ── Lotes ──────────────────────────────────────────────────────
        public async Task<List<LoteTrazabilidad>> GetLotesAsync(int empresaId, int? articuloId = null, EstadoLote? estado = null, bool? proximosCaducar = null)
        {
            var q = _context.LotesTrazabilidad.Include(l => l.Articulo).Where(l => l.EmpresaId == empresaId);
            if (articuloId.HasValue) q = q.Where(l => l.ArticuloId == articuloId.Value);
            if (estado.HasValue) q = q.Where(l => l.Estado == estado.Value);
            if (proximosCaducar == true) q = q.Where(l => l.FechaCaducidad != null && l.FechaCaducidad <= DateTime.Now.AddDays(30) && l.FechaCaducidad > DateTime.Now);
            return await q.OrderByDescending(l => l.FechaCreacion).ToListAsync();
        }

        public async Task<LoteTrazabilidad?> GetLoteAsync(int empresaId, int id)
            => await _context.LotesTrazabilidad.Include(l => l.Articulo).Include(l => l.MovimientosEntrada).Include(l => l.MovimientosSalida).FirstOrDefaultAsync(l => l.Id == id && l.EmpresaId == empresaId);

        public async Task<LoteTrazabilidad?> GetPorCodigoAsync(int empresaId, string codigo)
            => await _context.LotesTrazabilidad.FirstOrDefaultAsync(l => l.CodigoLote == codigo && l.EmpresaId == empresaId);

        public async Task<LoteTrazabilidad> CrearLoteAsync(LoteTrazabilidad dto, string? usuario)
        {
            if (await _context.LotesTrazabilidad.AnyAsync(l => l.CodigoLote == dto.CodigoLote && l.EmpresaId == dto.EmpresaId))
                throw new InvalidOperationException($"Código de lote {dto.CodigoLote} ya existe");
            dto.CantidadActual = dto.CantidadInicial;
            dto.FechaCreacion = DateTime.Now; dto.UsuarioCreacion = usuario;
            _context.LotesTrazabilidad.Add(dto);
            await _context.SaveChangesAsync();
            if (dto.EsPCC && !dto.TieneAnalisisOficial)
            {
                _context.AlertasTrazabilidad.Add(new AlertaTrazabilidad
                {
                    EmpresaId = dto.EmpresaId, LoteId = dto.Id,
                    Tipo = TipoAlertaTrazabilidad.AnalisisPendiente, Severidad = SeveridadAlerta.Alta,
                    Titulo = $"Lote {dto.CodigoLote} PCC sin análisis",
                    Descripcion = "Requiere análisis oficial antes de liberar (APPCC)"
                });
                await _context.SaveChangesAsync();
            }
            return dto;
        }

        public async Task ActualizarLoteAsync(LoteTrazabilidad dto, string? usuario)
        {
            var e = await _context.LotesTrazabilidad.FirstOrDefaultAsync(l => l.Id == dto.Id && l.EmpresaId == dto.EmpresaId)
                ?? throw new KeyNotFoundException("Lote no encontrado");
            e.CantidadActual = dto.CantidadActual; e.Estado = dto.Estado;
            e.FechaCaducidad = dto.FechaCaducidad; e.CondicionesAlmacenamiento = dto.CondicionesAlmacenamiento;
            e.TieneAnalisisOficial = dto.TieneAnalisisOficial; e.FechaModificacion = DateTime.Now; e.UsuarioModificacion = usuario;
            await _context.SaveChangesAsync();
        }

        public async Task<object> GetTrazabilidadCompletaAsync(int empresaId, int loteId)
        {
            var lote = await _context.LotesTrazabilidad.Include(l => l.Articulo).FirstOrDefaultAsync(l => l.Id == loteId && l.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Lote no encontrado");
            var movimientos = await _context.MovimientosLote.Where(m => m.LoteId == loteId && m.EmpresaId == empresaId).OrderBy(m => m.Fecha).ToListAsync();
            var alertas = await _context.AlertasTrazabilidad.Where(a => a.LoteId == loteId).ToListAsync();
            var retiradas = await _context.RetiradasLote.Where(r => r.LoteId == loteId).ToListAsync();
            var pasoAtras = new { lote.ProveedorId, lote.LoteProveedor, lote.DocumentoOrigen, lote.FechaRecepcion };
            var pasoAdelante = await _context.MovimientosLote.Where(m => m.LoteId == loteId && m.Tipo == TipoMovimientoLote.Expedicion).Select(m => new { m.ClienteId, m.NumeroDocumento, m.FechaEntrega }).ToListAsync();
            return new { Lote = lote, Movimientos = movimientos, Alertas = alertas, Retiradas = retiradas, UnPasoAtras = pasoAtras, UnPasoAdelante = pasoAdelante };
        }

        // ── Movimientos (stock) ────────────────────────────────────────
        public async Task<MovimientoLote> RegistrarMovimientoAsync(MovimientoLote dto, string? usuario)
        {
            var lote = await _context.LotesTrazabilidad.FirstOrDefaultAsync(l => l.Id == dto.LoteId && l.EmpresaId == dto.EmpresaId)
                ?? throw new KeyNotFoundException("Lote no válido");
            // Actualizar stock según tipo
            if (dto.Tipo == TipoMovimientoLote.Recepcion || dto.Tipo == TipoMovimientoLote.Devolucion || dto.Tipo == TipoMovimientoLote.Produccion)
                lote.CantidadActual += dto.Cantidad;
            else if (dto.Tipo == TipoMovimientoLote.Expedicion || dto.Tipo == TipoMovimientoLote.Merma || dto.Tipo == TipoMovimientoLote.Destruccion || dto.Tipo == TipoMovimientoLote.Retirada)
            {
                if (lote.CantidadDisponible < dto.Cantidad)
                    throw new InvalidOperationException($"Stock insuficiente. Disponible {lote.CantidadDisponible:N4}, solicitado {dto.Cantidad:N4}");
                lote.CantidadActual -= dto.Cantidad;
                if (lote.CantidadActual <= 0) lote.Estado = EstadoLote.Agotado;
            }
            dto.FechaCreacion = DateTime.Now; dto.UsuarioCreacion = usuario;
            _context.MovimientosLote.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Alertas ────────────────────────────────────────────────────
        public async Task<List<AlertaTrazabilidad>> GetAlertasAsync(int empresaId, bool soloPendientes = true, SeveridadAlerta? severidad = null)
        {
            var q = _context.AlertasTrazabilidad.Include(a => a.Lote).Where(a => a.EmpresaId == empresaId);
            if (soloPendientes) q = q.Where(a => !a.Resuelta);
            if (severidad.HasValue) q = q.Where(a => a.Severidad == severidad.Value);
            return await q.OrderByDescending(a => a.FechaAlerta).ToListAsync();
        }

        public async Task<AlertaTrazabilidad> MarcarLeidaAsync(int empresaId, int alertaId, string? usuario)
        {
            var a = await _context.AlertasTrazabilidad.FirstOrDefaultAsync(x => x.Id == alertaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException("Alerta no encontrada");
            a.Leida = true; a.FechaLectura = DateTime.Now; a.UsuarioLectura = usuario;
            await _context.SaveChangesAsync(); return a;
        }

        public async Task<AlertaTrazabilidad> ResolverAsync(int empresaId, int alertaId, string accion, string? usuario)
        {
            var a = await _context.AlertasTrazabilidad.FirstOrDefaultAsync(x => x.Id == alertaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException("Alerta no encontrada");
            a.Resuelta = true; a.FechaResolucion = DateTime.Now; a.UsuarioResolucion = usuario; a.AccionCorrectiva = accion;
            await _context.SaveChangesAsync(); return a;
        }

        public async Task<int> GenerarAlertasCaducidadAsync(int empresaId)
        {
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
            return creadas;
        }

        // ── Retiradas RASFF/AESAN ──────────────────────────────────────
        public async Task<RetiradaLote> CrearRetiradaAsync(RetiradaLote dto, string? usuario)
        {
            var lote = await _context.LotesTrazabilidad.FirstOrDefaultAsync(l => l.Id == dto.LoteId && l.EmpresaId == dto.EmpresaId)
                ?? throw new KeyNotFoundException("Lote no válido");
            dto.FechaDeteccion = dto.FechaDeteccion == default ? DateTime.Now : dto.FechaDeteccion;
            dto.FechaCreacion = DateTime.Now; dto.UsuarioResponsable = usuario;
            lote.Estado = EstadoLote.Retirado;
            _context.RetiradasLote.Add(dto);
            _context.AlertasTrazabilidad.Add(new AlertaTrazabilidad
            {
                EmpresaId = dto.EmpresaId, LoteId = dto.LoteId,
                Tipo = TipoAlertaTrazabilidad.RetiradaMercado, Severidad = SeveridadAlerta.Critica,
                Titulo = $"Retirada {dto.Severidad} lote {lote.CodigoLote}",
                Descripcion = dto.Motivo
            });
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<RetiradaLote> NotificarAesanAsync(int empresaId, int retiradaId, string expediente)
        {
            var r = await _context.RetiradasLote.FirstOrDefaultAsync(x => x.Id == retiradaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException("Retirada no encontrada");
            r.FechaNotificacionAutoridades = DateTime.Now; r.NumeroExpedienteAESAN = expediente;
            if (r.Severidad == SeveridadRetirada.ClaseI) r.NumeroNotificacionRASFF = $"RASFF-{DateTime.Now:yyyy}-{r.Id:D6}";
            await _context.SaveChangesAsync(); return r;
        }

        public async Task<RetiradaLote> CerrarRetiradaAsync(int empresaId, int retiradaId, string acciones)
        {
            var r = await _context.RetiradasLote.FirstOrDefaultAsync(x => x.Id == retiradaId && x.EmpresaId == empresaId) ?? throw new KeyNotFoundException("Retirada no encontrada");
            r.Estado = EstadoRetirada.Cerrada; r.FechaCierre = DateTime.Now; r.AccionesCorrectivas = acciones;
            await _context.SaveChangesAsync(); return r;
        }
    }
}
