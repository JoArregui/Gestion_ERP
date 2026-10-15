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

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == request.ProveedorId && p.IsActivo);
            if (proveedor == null)
                throw new InvalidOperationException("El proveedor no existe o está inactivo.");
            if (proveedor.BloqueadoCompras)
                throw new InvalidOperationException($"Proveedor bloqueado para compras: {proveedor.MotivoBloqueo ?? "sin motivo"}.");
            if (!proveedor.Homologado)
                throw new InvalidOperationException("El proveedor no está homologado. Homológalo antes de pedir (ficha de proveedor).");

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
        /// Procesa la recepción de un pedido de compra (albarán simple, sin factura).
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

                AplicarEntradaStock(pedido, numeroAlbaran);

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
        /// Recepción con validación cruzada pedido → albarán → factura (3-way matching, Fase C3).
        /// Compara el total de la factura del proveedor con el pedido dentro de la tolerancia %.
        /// </summary>
        public async Task<(bool success, string errorMessage)> RecepcionarConFacturaAsync(int pedidoId, string numeroAlbaran, string numeroFacturaProveedor, decimal totalFactura, decimal toleranciaPct = 2m)
        {
            if (string.IsNullOrWhiteSpace(numeroFacturaProveedor))
                return (false, "El número de factura del proveedor es obligatorio para el 3-way matching.");
            if (totalFactura <= 0)
                return (false, "El total de la factura debe ser positivo.");

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
                    return (false, "El pedido " + pedido.NumeroDocumento + " ya fue procesado anteriormente.");
                }

                var desviacion = pedido.Total == 0 ? 100m : Math.Abs(totalFactura - pedido.Total) / Math.Abs(pedido.Total) * 100m;
                if (desviacion > toleranciaPct)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Descuadre 3-way: pedido {pedido.Total:N2} vs factura {totalFactura:N2} (desviación {desviacion:N2}% > tolerancia {toleranciaPct:N2}%). Revise albarán y factura.");
                }

                AplicarEntradaStock(pedido, numeroAlbaran);

                pedido.IsContabilizado = true;
                pedido.NumeroAlbaran = numeroAlbaran;
                pedido.NumeroFacturaProveedor = numeroFacturaProveedor;
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

        /// <summary>Entrada de stock + PMP + Kardex compartida por ambas recepciones.</summary>
        private void AplicarEntradaStock(DocumentoComercial pedido, string numeroAlbaran)
        {
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

        // ── Homologación (Fase C2) ─────────────────────────────
        public async Task<Proveedor> HomologarProveedorAsync(int empresaId, int proveedorId, string? certificado, string? usuario)
        {
            var p = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == proveedorId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proveedor no encontrado");
            p.Homologado = true;
            p.CertificadoCalidad = certificado ?? p.CertificadoCalidad;
            p.FechaHomologacion = DateTime.Now;
            if (p.BloqueadoCompras) { p.BloqueadoCompras = false; p.MotivoBloqueo = null; }
            await _context.SaveChangesAsync();
            return p;
        }

        public async Task<Proveedor> BloquearProveedorAsync(int empresaId, int proveedorId, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Motivo obligatorio");
            var p = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == proveedorId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proveedor no encontrado");
            p.BloqueadoCompras = true;
            p.MotivoBloqueo = motivo;
            await _context.SaveChangesAsync();
            return p;
        }

        public async Task<Proveedor> EvaluarProveedorAsync(int empresaId, int proveedorId, int? calidad, int? plazo)
        {
            var p = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == proveedorId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proveedor no encontrado");
            if (calidad.HasValue) p.EvaluacionCalidad = Math.Clamp(calidad.Value, 0, 5);
            if (plazo.HasValue) p.EvaluacionPlazo = Math.Clamp(plazo.Value, 0, 5);
            await _context.SaveChangesAsync();
            return p;
        }

        // ── RFQ (Fase C1) ────────────────────────────────────
        public async Task<ERP.Domain.Entities.Compras.SolicitudOferta> CrearRFQAsync(int empresaId, List<(int articuloId, decimal cantidad)> lineas, DateTime? fechaLimite, string? notas, string? usuario)
        {
            if (lineas == null || lineas.Count == 0) throw new ArgumentException("La RFQ necesita al menos una línea");
            var articuloIds = lineas.Select(l => l.articuloId).Distinct().ToList();
            var count = await _context.Articulos.CountAsync(a => articuloIds.Contains(a.Id) && a.EmpresaId == empresaId && !a.IsDescatalogado);
            if (count != articuloIds.Count) throw new ArgumentException("Uno o más artículos no pertenecen a la empresa");

            var n = await _context.SolicitudesOferta.CountAsync(s => s.EmpresaId == empresaId);
            var rfq = new ERP.Domain.Entities.Compras.SolicitudOferta
            {
                EmpresaId = empresaId,
                Codigo = $"RFQ-{(n + 1):D4}",
                FechaLimite = fechaLimite,
                Notas = notas,
                UsuarioCreacion = usuario,
                Lineas = lineas.Select(l => new ERP.Domain.Entities.Compras.LineaSolicitudOferta { ArticuloId = l.articuloId, Cantidad = l.cantidad }).ToList()
            };
            _context.SolicitudesOferta.Add(rfq);
            await _context.SaveChangesAsync();
            return rfq;
        }

        public async Task<ERP.Domain.Entities.Compras.SolicitudOferta> EnviarRFQAsync(int empresaId, int rfqId, List<int> proveedorIds)
        {
            var rfq = await _context.SolicitudesOferta.FirstOrDefaultAsync(s => s.Id == rfqId && s.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("RFQ no encontrada");
            if (rfq.Estado != ERP.Domain.Entities.Compras.EstadoRFQ.Borrador) throw new InvalidOperationException("Solo borradores se envían");
            if (proveedorIds == null || proveedorIds.Count == 0) throw new ArgumentException("Indique al menos un proveedor invitado");
            var validos = await _context.Proveedores.Where(p => proveedorIds.Contains(p.Id) && p.EmpresaId == empresaId && p.IsActivo && !p.BloqueadoCompras).Select(p => p.Id).ToListAsync();
            if (validos.Count == 0) throw new ArgumentException("Ningún proveedor invitado es válido (activo y no bloqueado)");
            rfq.Estado = ERP.Domain.Entities.Compras.EstadoRFQ.Enviada;
            rfq.ProveedoresInvitadosJson = System.Text.Json.JsonSerializer.Serialize(validos);
            await _context.SaveChangesAsync();
            return rfq;
        }

        public async Task<ERP.Domain.Entities.Compras.OfertaProveedor> RegistrarOfertaAsync(int empresaId, int rfqId, int proveedorId, List<(int articuloId, decimal cantidad, decimal precio)> lineas, int? plazoDias, string? notas)
        {
            var rfq = await _context.SolicitudesOferta.Include(s => s.Lineas).FirstOrDefaultAsync(s => s.Id == rfqId && s.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("RFQ no encontrada");
            if (rfq.Estado != ERP.Domain.Entities.Compras.EstadoRFQ.Enviada) throw new InvalidOperationException("La RFQ no está enviada");
            if (!await _context.Proveedores.AnyAsync(p => p.Id == proveedorId && p.EmpresaId == empresaId && p.IsActivo))
                throw new ArgumentException("Proveedor no válido");
            if (lineas == null || lineas.Count == 0) throw new ArgumentException("La oferta necesita líneas");
            var pedidas = rfq.Lineas.Select(l => l.ArticuloId).ToHashSet();
            foreach (var l in lineas)
                if (!pedidas.Contains(l.articuloId) || l.cantidad <= 0 || l.precio < 0)
                    throw new ArgumentException("Línea de oferta inválida (artículo no pedido o cantidad/precio incorrectos)");

            var oferta = new ERP.Domain.Entities.Compras.OfertaProveedor
            {
                SolicitudId = rfqId,
                ProveedorId = proveedorId,
                PlazoEntregaDias = plazoDias,
                Notas = notas,
                Lineas = lineas.Select(l => new ERP.Domain.Entities.Compras.LineaOferta { ArticuloId = l.articuloId, Cantidad = l.cantidad, PrecioUnitario = l.precio }).ToList()
            };
            _context.OfertasProveedor.Add(oferta);
            await _context.SaveChangesAsync();
            return oferta;
        }

        /// <summary>Comparativa: mejor precio por artículo + total por oferta.</summary>
        public async Task<ERP.Domain.DTOs.Compras.ComparativaRFQ> GetComparativaAsync(int empresaId, int rfqId)
        {
            var rfq = await _context.SolicitudesOferta
                .Include(s => s.Lineas).ThenInclude(l => l.Articulo)
                .Include(s => s.Ofertas).ThenInclude(o => o.Proveedor)
                .Include(s => s.Ofertas).ThenInclude(o => o.Lineas).ThenInclude(l => l.Articulo)
                .FirstOrDefaultAsync(s => s.Id == rfqId && s.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("RFQ no encontrada");

            var lineas = rfq.Lineas.Select(l => new ERP.Domain.DTOs.Compras.LineaComparativa(
                l.ArticuloId,
                l.Articulo?.Descripcion ?? $"Art.{l.ArticuloId}",
                l.Cantidad,
                rfq.Ofertas
                    .SelectMany(o => o.Lineas.Where(ol => ol.ArticuloId == l.ArticuloId)
                        .Select(ol => new ERP.Domain.DTOs.Compras.PrecioOferta(o.ProveedorId, o.Proveedor?.RazonSocial ?? $"Prov.{o.ProveedorId}", ol.PrecioUnitario)))
                    .OrderBy(p => p.Precio)
                    .ToList())).ToList();

            var totales = rfq.Ofertas.Select(o => new ERP.Domain.DTOs.Compras.TotalOferta(
                o.Id, o.ProveedorId, o.Proveedor?.RazonSocial ?? $"Prov.{o.ProveedorId}",
                o.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario), o.PlazoEntregaDias, o.Estado.ToString())).OrderBy(t => t.Total).ToList();

            return new ERP.Domain.DTOs.Compras.ComparativaRFQ(rfq.Id, rfq.Codigo, rfq.Estado.ToString(), lineas, totales);
        }

        /// <summary>Adjudica una oferta: la marca seleccionada, genera el Pedido y cierra la RFQ.</summary>
        public async Task<DocumentoComercial> AdjudicarAsync(int empresaId, int rfqId, int ofertaId)
        {
            var rfq = await _context.SolicitudesOferta.Include(s => s.Ofertas).ThenInclude(o => o.Lineas)
                .FirstOrDefaultAsync(s => s.Id == rfqId && s.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("RFQ no encontrada");
            if (rfq.Estado != ERP.Domain.Entities.Compras.EstadoRFQ.Enviada) throw new InvalidOperationException("La RFQ no está pendiente de adjudicar");
            var oferta = rfq.Ofertas.FirstOrDefault(o => o.Id == ofertaId) ?? throw new KeyNotFoundException("Oferta no encontrada");

            var pedido = await CrearPedidoAsync(new PedidoCompraRequest
            {
                ProveedorId = oferta.ProveedorId,
                Lineas = oferta.Lineas.Select(l => new LineaPedidoCompraRequest { ArticuloId = l.ArticuloId, Cantidad = l.Cantidad, PrecioUnitario = l.PrecioUnitario }).ToList()
            }, empresaId);

            oferta.Estado = ERP.Domain.Entities.Compras.EstadoOferta.Seleccionada;
            foreach (var otra in rfq.Ofertas.Where(o => o.Id != ofertaId))
                otra.Estado = ERP.Domain.Entities.Compras.EstadoOferta.Descartada;
            rfq.Estado = ERP.Domain.Entities.Compras.EstadoRFQ.Adjudicada;
            rfq.PedidoAdjudicadoId = pedido.Id;
            await _context.SaveChangesAsync();
            return pedido;
        }
    }
}
