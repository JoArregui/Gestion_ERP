using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ERP.Services
{
    public class FacturacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly StockService _stockService;
        private readonly VerifactuService _verifactuService;

        public FacturacionService(ApplicationDbContext context, StockService stockService, VerifactuService verifactuService)
        {
            _context = context;
            _stockService = stockService;
            _verifactuService = verifactuService;
        }

        public async Task<bool> RegistrarFacturaVentaAsync(DocumentoComercial factura)
        {
            // Usamos una transacción para que si falla el stock, no se guarde la factura
            // Reutilizamos la transacción del ciclo documental cuando exista.
            var transaction = _context.Database.CurrentTransaction is null
                ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
                : null;

            try
            {
                // 1. Validaciones mínimas de negocio
                if (factura.Lineas == null || !factura.Lineas.Any())
                    throw new Exception("La factura no tiene líneas.");

                // 2. Estado y guardado
                factura.Estado = EstadoDocumento.Emitido;
                _context.Documentos.Add(factura);
                await _context.SaveChangesAsync();

                // 3. Delegar la actualización de Stock al servicio especializado
                // Este servicio ya crea los MovimientoStock con tus campos correctos
                // (TipoMovimiento, ReferenciaDocumento, etc.)
                await _stockService.ProcesarMovimientoStock(factura.Id);

                if (!factura.EsCompra && !await _context.Vencimientos.AnyAsync(v => v.DocumentoId == factura.Id))
                {
                    _context.Vencimientos.Add(new Vencimiento
                    {
                        DocumentoId = factura.Id,
                        EmpresaId = factura.EmpresaId,
                        Importe = factura.Total,
                        FechaVencimiento = factura.Fecha.AddDays(30),
                        Estado = "Pendiente",
                        MetodoPago = factura.MetodoPago
                    });
                    await _context.SaveChangesAsync();
                }

                if (!factura.EsCompra && !await _context.RegistrosVerifactu.AnyAsync(r => r.DocumentoId == factura.Id))
                    await _verifactuService.GenerarRegistroAltaAsync(factura);

                // 4. Consolidar cambios
                if (transaction is not null)
                    await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log del error (opcional)
                Console.WriteLine($"Error en facturación: {ex.Message}");
                if (transaction is not null)
                    await transaction.RollbackAsync();
                return false;
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync();
            }
        }
    }
}
