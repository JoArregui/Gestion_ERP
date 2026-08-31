using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.DTOs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Tests;

public sealed class FacturacionWorkflowTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public FacturacionWorkflowTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task RegistrarFacturaVentaAsync_crea_un_vencimiento_pendiente()
    {
        var empresa = await CrearEmpresaAsync();
        var articulo = await CrearArticuloAsync();
        var factura = CrearFactura(empresa.Id, articulo.Id, esCompra: false);
        var service = CrearFacturacionService();

        var resultado = await service.RegistrarFacturaVentaAsync(factura);

        Assert.True(resultado);
        var vencimiento = await _context.Vencimientos.SingleAsync();
        Assert.Equal(factura.Id, vencimiento.DocumentoId);
        Assert.Equal(factura.Total, vencimiento.Importe);
        Assert.Equal("Pendiente", vencimiento.Estado);
        Assert.Equal(factura.Fecha.AddDays(30), vencimiento.FechaVencimiento);
        var registro = await _context.RegistrosVerifactu.SingleAsync();
        Assert.Equal(factura.Id, registro.DocumentoId);
        Assert.Equal("Pendiente", registro.EstadoRemision);
    }

    [Fact]
    public async Task RegistrarFacturaVentaAsync_no_crea_vencimiento_para_factura_de_compra()
    {
        var empresa = await CrearEmpresaAsync();
        var articulo = await CrearArticuloAsync();
        var factura = CrearFactura(empresa.Id, articulo.Id, esCompra: true);
        var service = CrearFacturacionService();

        var resultado = await service.RegistrarFacturaVentaAsync(factura);

        Assert.True(resultado);
        Assert.Empty(await _context.Vencimientos.ToListAsync());
        Assert.Empty(await _context.RegistrosVerifactu.ToListAsync());
    }

    [Fact]
    public async Task ConvertirDocumento_a_factura_crea_vencimiento_y_confirma_el_stock_reservado()
    {
        var empresa = await CrearEmpresaAsync();
        var articulo = await CrearArticuloAsync(stock: 10m);
        var albaran = CrearFactura(empresa.Id, articulo.Id, esCompra: false);
        albaran.Tipo = TipoDocumento.Albaran;
        albaran.NumeroDocumento = "ALB-2026-00001";
        _context.Documentos.Add(albaran);
        await _context.SaveChangesAsync();
        await new StockService(_context).ProcesarMovimientoStock(albaran.Id);

        var stock = new StockService(_context);
        var ciclo = new CicloFacturacionService(_context, new FacturacionService(_context, stock, new VerifactuService(_context)), stock);

        var factura = await ciclo.ConvertirDocumento(albaran.Id, TipoDocumento.Factura);

        Assert.Equal(TipoDocumento.Factura, factura.Tipo);
        Assert.Single(await _context.Vencimientos.Where(v => v.DocumentoId == factura.Id).ToListAsync());
        var articuloActualizado = await _context.Articulos.IgnoreQueryFilters().SingleAsync(a => a.Id == articulo.Id);
        Assert.Equal(8m, articuloActualizado.Stock);
        Assert.Equal(0m, articuloActualizado.StockReservado);
    }

    [Fact]
    public async Task CrearDocumento_numera_por_empresa_tipo_y_ano()
    {
        var primeraEmpresa = await CrearEmpresaAsync();
        var segundaEmpresa = await CrearEmpresaAsync();
        var primerArticulo = await CrearArticuloAsync();
        var segundoArticulo = await CrearArticuloAsync();
        var stock = new StockService(_context);
        var ciclo = new CicloFacturacionService(_context, new FacturacionService(_context, stock, new VerifactuService(_context)), stock);

        var primero = await ciclo.CrearDocumento(CrearPresupuesto(primeraEmpresa.Id, primerArticulo.Id));
        var segundo = await ciclo.CrearDocumento(CrearPresupuesto(primeraEmpresa.Id, primerArticulo.Id));
        var otraEmpresa = await ciclo.CrearDocumento(CrearPresupuesto(segundaEmpresa.Id, segundoArticulo.Id));

        Assert.Equal("PRE-2026-00001", primero.NumeroDocumento);
        Assert.Equal("PRE-2026-00002", segundo.NumeroDocumento);
        Assert.Equal("PRE-2026-00001", otraEmpresa.NumeroDocumento);
    }

    [Fact]
    public async Task ConvertirDocumento_rechaza_retroceder_en_el_ciclo_documental()
    {
        var empresa = await CrearEmpresaAsync();
        var articulo = await CrearArticuloAsync();
        var factura = CrearFactura(empresa.Id, articulo.Id, esCompra: false);
        _context.Documentos.Add(factura);
        await _context.SaveChangesAsync();
        var stock = new StockService(_context);
        var ciclo = new CicloFacturacionService(_context, new FacturacionService(_context, stock, new VerifactuService(_context)), stock);

        var exception = await Assert.ThrowsAsync<Exception>(() => ciclo.ConvertirDocumento(factura.Id, TipoDocumento.Albaran));

        Assert.Contains("solo admite avances", exception.Message);
    }

    [Fact]
    public void CalcularHuellaAlta_coincide_con_el_vector_publicado_por_la_Aeat()
    {
        var registro = new RegistroVerifactu
        {
            NifEmisor = "89890001K",
            NumeroFactura = "12345678/G33",
            FechaExpedicion = new DateTime(2024, 1, 1),
            TipoFactura = "F1",
            CuotaTotal = 12.35m,
            ImporteTotal = 123.45m,
            FechaHoraHusoGeneracion = new DateTimeOffset(2024, 1, 1, 19, 20, 30, TimeSpan.FromHours(1))
        };

        var huella = VerifactuService.CalcularHuellaAlta(registro);

        Assert.Equal("3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60", huella);
    }

    [Fact]
    public async Task AjustarStockAsync_registra_auditoria_y_respeta_el_stock_reservado()
    {
        var empresa = await CrearEmpresaAsync();
        var articulo = await CrearArticuloAsync(stock: 10m);
        articulo.EmpresaId = empresa.Id;
        articulo.StockReservado = 4m;
        await _context.SaveChangesAsync();
        var servicio = new StockService(_context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.AjustarStockAsync(new AjusteStockDTO
        {
            ArticuloId = articulo.Id,
            NuevoStock = 3m,
            TerminalId = "TEST"
        }));

        var stock = await servicio.AjustarStockAsync(new AjusteStockDTO
        {
            ArticuloId = articulo.Id,
            NuevoStock = 12m,
            Motivo = "Conteo anual",
            TerminalId = "TEST"
        });

        Assert.Equal(12m, stock);
        var movimiento = await _context.MovimientosStock.SingleAsync();
        Assert.Equal("AJUSTE_ENTRADA", movimiento.TipoMovimiento);
        Assert.Equal(empresa.Id, movimiento.EmpresaId);
        Assert.Equal(2m, movimiento.Cantidad);
    }

    private FacturacionService CrearFacturacionService() =>
        new(_context, new StockService(_context), new VerifactuService(_context));

    private async Task<Empresa> CrearEmpresaAsync()
    {
        var empresa = new Empresa
        {
            NombreComercial = "Empresa de pruebas",
            RazonSocial = "Empresa de pruebas",
            CIF = $"B{Guid.NewGuid():N}"[..10]
        };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        return empresa;
    }

    private async Task<Articulo> CrearArticuloAsync(decimal stock = 2m)
    {
        var articulo = new Articulo
        {
            EmpresaId = 1,
            Codigo = Guid.NewGuid().ToString("N"),
            Descripcion = "Artículo de pruebas",
            Stock = stock,
            PrecioCompra = 10m,
            PrecioVenta = 20m,
            PorcentajeIva = 21m,
            FamiliaId = await CrearFamiliaAsync()
        };
        _context.Articulos.Add(articulo);
        await _context.SaveChangesAsync();
        return articulo;
    }

    private async Task<int> CrearFamiliaAsync()
    {
        var familia = new Familia { Nombre = $"Familia-{Guid.NewGuid():N}" };
        _context.Familia.Add(familia);
        await _context.SaveChangesAsync();
        return familia.Id;
    }

    private static DocumentoComercial CrearFactura(int empresaId, int articuloId, bool esCompra) => new()
    {
        EmpresaId = empresaId,
        EsCompra = esCompra,
        Tipo = TipoDocumento.Factura,
        NumeroDocumento = $"FAC-2026-{Guid.NewGuid():N}",
        Fecha = new DateTime(2026, 8, 21),
        BaseImponible = 20m,
        TotalIva = 4.2m,
        Total = 24.2m,
        Lineas =
        {
            new DocumentoLinea
            {
                ArticuloId = articuloId,
                Cantidad = 2m,
                PrecioUnitario = 10m,
                PorcentajeIva = 21m,
                DescripcionArticulo = "Artículo de pruebas"
            }
        }
    };

    private static DocumentoComercial CrearPresupuesto(int empresaId, int articuloId) => new()
    {
        EmpresaId = empresaId,
        Tipo = TipoDocumento.Presupuesto,
        Fecha = new DateTime(2026, 8, 21),
        Lineas =
        {
            new DocumentoLinea
            {
                ArticuloId = articuloId,
                Cantidad = 1m,
                PrecioUnitario = 10m,
                PorcentajeIva = 21m,
                DescripcionArticulo = "Artículo de pruebas"
            }
        }
    };

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
