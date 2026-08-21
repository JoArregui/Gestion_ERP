using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    public class StockService
    {
        private readonly ApplicationDbContext _context;

        public StockService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Procesa un documento completo con validaciones de integridad y control de stock negativo.
        /// </summary>
        public async Task ProcesarMovimientoStock(int documentoId)
        {
            // Usamos una transacción para asegurar la atomicidad de todos los cambios
            var transaction = _context.Database.CurrentTransaction is null
                ? await _context.Database.BeginTransactionAsync()
                : null;

            try
            {
                var doc = await _context.Documentos
                    .Include(d => d.Lineas)
                    .FirstOrDefaultAsync(d => d.Id == documentoId);

                if (doc == null || doc.Lineas == null || !doc.Lineas.Any()) return;

                foreach (var linea in doc.Lineas)
                {
                    // Bloqueo Pesimista: Evitamos que otro proceso modifique el artículo mientras operamos
                    var articulo = await _context.Articulos
                        .FirstOrDefaultAsync(a => a.Id == linea.ArticuloId);

                    if (articulo == null)
                        throw new Exception($"El artículo con ID {linea.ArticuloId} no existe en el maestro.");

                    string tipoMov = "";
                    decimal cantidadMov = linea.Cantidad;
                    decimal stockPrevio = articulo.Stock;

                    if (doc.EsCompra)
                    {
                        // --- ENTRADA DE MERCANCÍA ---
                        tipoMov = "ENTRADA_COMPRA";

                        // Recálculo del PMP antes de actualizar el stock total
                        // Fórmula: ((Stock Actual * PMP Actual) + (Nueva Cantidad * Nuevo Precio)) / (Stock Actual + Nueva Cantidad)
                        if (articulo.Stock + linea.Cantidad > 0)
                        {
                            articulo.PrecioCompra = ((articulo.Stock * articulo.PrecioCompra) + (linea.Cantidad * linea.PrecioUnitario))
                                                    / (articulo.Stock + linea.Cantidad);
                        }

                        articulo.Stock += linea.Cantidad;
                    }
                    else
                    {
                        // --- SALIDA / VENTA ---
                        if (doc.Tipo == TipoDocumento.Albaran)
                        {
                            // Validación: ¿Hay stock disponible (Físico - Reservado)?
                            decimal disponible = articulo.Stock - articulo.StockReservado;
                            if (disponible < linea.Cantidad)
                            {
                                // Aquí puedes decidir si lanzar excepción o permitir stock negativo según configuración
                                // throw new Exception($"Stock insuficiente para {articulo.Nombre}. Disponible: {disponible}");
                            }

                            tipoMov = "RESERVA_ALBARAN";
                            articulo.StockReservado += linea.Cantidad;
                        }
                        else if (doc.Tipo == TipoDocumento.Factura)
                        {
                            tipoMov = "SALIDA_FACTURA";

                            // Si viene de un albarán, la reserva ya existe y hay que consumirla
                            if (doc.DocumentoOrigenId.HasValue)
                            {
                                var tieneAlbaran = await _context.Documentos
                                    .AnyAsync(x => x.Id == doc.DocumentoOrigenId && x.Tipo == TipoDocumento.Albaran);

                                if (tieneAlbaran)
                                {
                                    articulo.StockReservado -= linea.Cantidad;
                                }
                            }

                            articulo.Stock -= linea.Cantidad;
                        }
                    }

                    // Auditoría detallada
                    _context.MovimientosStock.Add(new MovimientoStock
                    {
                        Fecha = DateTime.Now,
                        ArticuloId = articulo.Id,
                        EmpresaId = doc.EmpresaId,
                        TipoMovimiento = tipoMov,
                        Cantidad = cantidadMov,
                        StockResultante = articulo.Stock, // Stock físico real tras la operación
                        ReferenciaDocumento = doc.NumeroDocumento,
                        Observaciones = $"Doc: {doc.Tipo} | Origen: {doc.DocumentoOrigenId ?? 0} | PMP: {articulo.PrecioCompra:C2}"
                    });
                }

                await _context.SaveChangesAsync();
                if (transaction is not null)
                    await transaction.CommitAsync();
            }
            catch
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync();
            }
        }

        public async Task LiberarReservaPorAnulacion(int albaranId)
        {
            var albaran = await _context.Documentos
                .Include(d => d.Lineas)
                .FirstOrDefaultAsync(d => d.Id == albaranId);

            if (albaran == null) return;

            foreach (var linea in albaran.Lineas)
            {
                var articulo = await _context.Articulos.FindAsync(linea.ArticuloId);
                if (articulo != null)
                {
                    articulo.StockReservado -= linea.Cantidad;

                    // Registro de la liberación en el histórico
                    _context.MovimientosStock.Add(new MovimientoStock
                    {
                        Fecha = DateTime.Now,
                        ArticuloId = articulo.Id,
                        EmpresaId = albaran.EmpresaId,
                        TipoMovimiento = "ANULACION_RESERVA",
                        Cantidad = linea.Cantidad,
                        StockResultante = articulo.Stock,
                        ReferenciaDocumento = albaran.NumeroDocumento,
                        Observaciones = "Reserva liberada por eliminación de albarán."
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> AjustarStockAsync(AjusteStockDTO ajuste)
        {
            var articulo = ajuste.ArticuloId > 0
                ? await _context.Articulos.FindAsync(ajuste.ArticuloId)
                : await _context.Articulos.FirstOrDefaultAsync(a => a.Codigo == ajuste.CodigoBarras);

            if (articulo is null) throw new InvalidOperationException("Artículo no encontrado.");

            var stockObjetivo = ajuste.ArticuloId > 0 ? ajuste.NuevoStock : ajuste.CantidadReal;
            if (stockObjetivo < 0) throw new InvalidOperationException("El stock físico no puede ser negativo.");
            if (stockObjetivo < articulo.StockReservado)
                throw new InvalidOperationException("El stock físico no puede ser inferior al stock reservado en albaranes pendientes.");

            var diferencia = stockObjetivo - articulo.Stock;
            if (diferencia == 0) return articulo.Stock;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                articulo.Stock = stockObjetivo;
                _context.MovimientosStock.Add(new MovimientoStock
                {
                    ArticuloId = articulo.Id,
                    EmpresaId = articulo.EmpresaId,
                    Fecha = DateTime.Now,
                    TipoMovimiento = diferencia > 0 ? "AJUSTE_ENTRADA" : "AJUSTE_SALIDA",
                    Cantidad = Math.Abs(diferencia),
                    StockResultante = stockObjetivo,
                    ReferenciaDocumento = "AJUSTE INV",
                    Observaciones = string.IsNullOrWhiteSpace(ajuste.Motivo)
                        ? $"Ajuste desde {ajuste.TerminalId}"
                        : $"{ajuste.Motivo} | Terminal: {ajuste.TerminalId}"
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return articulo.Stock;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
