using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.DTOs;

namespace ERP.Services
{
    public class ComprasService
    {
        private readonly ApplicationDbContext _context;

        public ComprasService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DocumentoComercial> CrearPedidoAsync(PedidoCompraRequest request, int empresaId)
        {
            if (request.ProveedorId <= 0 || request.Lineas.Count == 0)
                throw new InvalidOperationException("El pedido debe incluir proveedor y al menos una línea.");

            var proveedorExiste = await _context.Proveedores
                .AnyAsync(p => p.Id == request.ProveedorId && p.IsActivo);
            if (!proveedorExiste)
                throw new InvalidOperationException("El proveedor no existe o está inactivo.");

            var articuloIds = request.Lineas.Select(l => l.ArticuloId).Distinct().ToList();
            var articulos = await _context.Articulos
                .Where(a => articuloIds.Contains(a.Id) && a.EmpresaId == empresaId && !a.IsDescatalogado)
                .ToDictionaryAsync(a => a.Id);

            if (articulos.Count != articuloIds.Count)
                throw new InvalidOperationException("Uno o más artículos no pertenecen a la empresa o no están disponibles.");

            var lineas = request.Lineas.Select(linea =>
            {
                if (linea.Cantidad <= 0 || linea.PrecioUnitario < 0)
                    throw new InvalidOperationException("Las cantidades deben ser mayores que cero y los precios no pueden ser negativos.");

                var articulo = articulos[linea.ArticuloId];
                return new DocumentoLinea
                {
                    ArticuloId = articulo.Id,
                    DescripcionArticulo = articulo.Descripcion,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = linea.PrecioUnitario,
                    PorcentajeIva = articulo.PorcentajeIva
                };
            }).ToList();

            var baseImponible = lineas.Sum(l => l.Cantidad * l.PrecioUnitario);
            var totalIva = lineas.Sum(l => l.Cantidad * l.PrecioUnitario * l.PorcentajeIva / 100m);
            var pedido = new DocumentoComercial
            {
                EmpresaId = empresaId,
                ProveedorId = request.ProveedorId,
                EsCompra = true,
                Tipo = TipoDocumento.Pedido,
                Fecha = DateTime.Now,
                NumeroDocumento = $"PED-{DateTime.Now:yyyyMMdd-HHmmssfff}",
                BaseImponible = baseImponible,
                TotalIva = totalIva,
                Total = baseImponible + totalIva,
                Lineas = lineas
            };

            _context.Documentos.Add(pedido);
            await _context.SaveChangesAsync();
            return pedido;
        }

        /// <summary>
        /// Procesa la recepción de un pedido de compra.
        /// </summary>
        public async Task<(bool success, string errorMessage)> RecepcionarPedido(int pedidoId, string numeroAlbaran)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pedido = await _context.Documentos
                    .Include(d => d.Lineas)
                        .ThenInclude(l => l.Articulo)
                    .FirstOrDefaultAsync(d => d.Id == pedidoId && d.Tipo == TipoDocumento.Pedido);

                if (pedido == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "El pedido con ID " + pedidoId + " no existe en el sistema.");
                }

                if (pedido.IsContabilizado)
                {
                    await transaction.RollbackAsync();
                    return (false, "El pedido " + pedido.NumeroDocumento + " ya fue procesado anteriormente. No se puede volver a recepcionar.");
                }

                foreach (var linea in pedido.Lineas)
                {
                    var articulo = linea.Articulo;
                    if (articulo == null) continue;

                    // Recalcular PMP
                    decimal valorActual = articulo.Stock * articulo.PrecioCompra;
                    decimal valorNuevaEntrada = linea.Cantidad * linea.PrecioUnitario;
                    decimal nuevoStockTotal = articulo.Stock + linea.Cantidad;

                    if (nuevoStockTotal > 0)
                    {
                        articulo.PrecioCompra = (valorActual + valorNuevaEntrada) / nuevoStockTotal;
                    }

                    articulo.Stock += linea.Cantidad;

                    // Registro en Kardex
                    var movimiento = new MovimientoStock
                    {
                        ArticuloId = articulo.Id,
                        EmpresaId = articulo.EmpresaId,
                        Fecha = DateTime.Now,
                        TipoMovimiento = "ENTRADA",
                        Cantidad = linea.Cantidad,
                        StockResultante = articulo.Stock,
                        ReferenciaDocumento = $"ALB: {numeroAlbaran}",
                        Observaciones = $"Recepción Pedido Compra Nº {pedido.NumeroDocumento}"
                    };

                    _context.MovimientosStock.Add(movimiento);
                }

                pedido.IsContabilizado = true;
                pedido.NumeroAlbaran = numeroAlbaran;
                pedido.FechaRecepcion = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                return (false, "Error de integridad de datos: " + ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "Error inesperado: " + ex.Message);
            }
        }

        /// <summary>
        /// Genera pedidos automáticos calculando el Total mediante la suma de Subtotales.
        /// </summary>
        public async Task<int> GenerarPedidoDesdeAlertas(List<AlertaStockDTO> alertas)
        {
            if (alertas == null || !alertas.Any()) return 0;

            int pedidosGenerados = 0;
            var alertasPorProveedor = alertas.GroupBy(a => a.ProveedorId);

            foreach (var grupo in alertasPorProveedor)
            {
                var proveedorId = grupo.Key;

                var nuevoPedido = new DocumentoComercial
                {
                    ProveedorId = proveedorId,
                    EmpresaId = 1,
                    Fecha = DateTime.Now,
                    EsCompra = true,
                    Tipo = TipoDocumento.Pedido,
                    IsContabilizado = false,
                    NumeroDocumento = $"PAUTO-{DateTime.Now:yyyyMMdd-HHmm}",
                    Lineas = new List<DocumentoLinea>()
                };

                foreach (var item in grupo)
                {
                    var articulo = await _context.Articulos.FindAsync(item.ArticuloId);
                    if (articulo == null) continue;

                    var linea = new DocumentoLinea
                    {
                        ArticuloId = articulo.Id,
                        Cantidad = item.CantidadAReponer,
                        PrecioUnitario = articulo.PrecioCompra,
                        DescripcionArticulo = articulo.Descripcion,
                        PorcentajeIva = articulo.PorcentajeIva // Corregido: ya no requiere casteo a double
                    };

                    nuevoPedido.Lineas.Add(linea);
                }

                // Cálculo automático del Total basado en las líneas recién agregadas
                nuevoPedido.Total = nuevoPedido.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario);

                // Cálculo de Base e IVA
                nuevoPedido.BaseImponible = nuevoPedido.Total / 1.21m;
                nuevoPedido.TotalIva = nuevoPedido.Total - nuevoPedido.BaseImponible;

                _context.Documentos.Add(nuevoPedido);
                pedidosGenerados++;
            }

            await _context.SaveChangesAsync();
            return pedidosGenerados;
        }
    }
}
