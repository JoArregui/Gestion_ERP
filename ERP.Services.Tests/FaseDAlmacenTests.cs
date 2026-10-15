using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Almacen;
using EntAlm = ERP.Domain.Entities.Almacen.Almacen;
using ERP.Services;
using ERP.Services.Almacen;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Tests;

/// <summary>Fase D: multialmacén, ubicaciones, reserva/consumo, picking y reposición.</summary>
public sealed class FaseDAlmacenTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public FaseDAlmacenTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private async Task<(Empresa empresa, Articulo art)> BaseAsync(decimal stock = 100m, decimal minimo = 0m)
    {
        var empresa = new Empresa { NombreComercial = "T", RazonSocial = "T", CIF = "B12345678" };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        var familia = new Familia { Nombre = "F", EmpresaId = empresa.Id };
        _context.Familia.Add(familia);
        await _context.SaveChangesAsync();
        var art = new Articulo { EmpresaId = empresa.Id, FamiliaId = familia.Id, Codigo = "A1", Descripcion = "A1", Stock = stock, StockMinimo = minimo, PrecioCompra = 10m, PrecioVenta = 20m, PorcentajeIva = 21m, CodigoBarras = "8412345678905", UnidadMedida = "UDS" };
        _context.Articulos.Add(art);
        await _context.SaveChangesAsync();
        return (empresa, art);
    }

    private async Task<(EntAlm alm, Ubicacion u1, Ubicacion u2)> NavesAsync(int empresaId)
    {
        var svc = new AlmacenService(_context);
        var alm = await svc.CrearAlmacenAsync(new EntAlm { EmpresaId = empresaId, Nombre = "Nave 1" });
        Assert.True(alm.EsPrincipal); // el primero es principal
        var u1 = await svc.CrearUbicacionAsync(empresaId, new Ubicacion { AlmacenId = alm.Id, Pasillo = "A", Estanteria = "01", Nivel = "01" });
        var u2 = await svc.CrearUbicacionAsync(empresaId, new Ubicacion { AlmacenId = alm.Id, Pasillo = "A", Estanteria = "02", Nivel = "01" });
        Assert.Equal("A-01-01", u1.Codigo);
        return (alm, u1, u2);
    }

    [Fact]
    public async Task Ubicar_y_mover_cuadran_con_sin_ubicar()
    {
        var (empresa, art) = await BaseAsync();
        var svc = new AlmacenService(_context);
        var (_, u1, u2) = await NavesAsync(empresa.Id);

        // EAN persistido
        Assert.Equal("8412345678905", (await _context.Articulos.FirstAsync(a => a.Id == art.Id)).CodigoBarras);

        await svc.UbicarStockAsync(empresa.Id, art.Id, u1.Id, 60m);
        Assert.Equal(40m, await svc.SinUbicarAsync(empresa.Id, art.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UbicarStockAsync(empresa.Id, art.Id, u2.Id, 50m));

        await svc.MoverStockAsync(empresa.Id, art.Id, u1.Id, u2.Id, 20m);
        var det = await svc.GetDetalleAsync(empresa.Id, art.Id);
        Assert.Equal(100m, det.StockTotal);
        Assert.Equal(60m, det.Ubicado);
        Assert.Equal(40m, det.SinUbicar);
        Assert.Equal(40m, det.Huecos.First(h => h.UbicacionId == u1.Id).Cantidad);
        Assert.Equal(20m, det.Huecos.First(h => h.UbicacionId == u2.Id).Cantidad);
    }

    [Fact]
    public async Task Reserva_y_consumo_de_albaran_a_factura_tocan_ubicaciones()
    {
        var (empresa, art) = await BaseAsync();
        var alsvc = new AlmacenService(_context);
        var (_, u1, _) = await NavesAsync(empresa.Id);
        await alsvc.UbicarStockAsync(empresa.Id, art.Id, u1.Id, 50m);
        var stock = new StockService(_context, alsvc);

        var albaran = new DocumentoComercial
        {
            EmpresaId = empresa.Id, EsCompra = false, Tipo = TipoDocumento.Albaran,
            NumeroDocumento = "ALB-1", Fecha = DateTime.Today,
            Lineas = new() { new DocumentoLinea { ArticuloId = art.Id, DescripcionArticulo = "A1", Cantidad = 10m, PrecioUnitario = 20m, PorcentajeIva = 21m } }
        };
        _context.Documentos.Add(albaran);
        await _context.SaveChangesAsync();
        await stock.ProcesarMovimientoStock(albaran.Id);

        var det = await alsvc.GetDetalleAsync(empresa.Id, art.Id);
        Assert.Equal(10m, det.Huecos.First(h => h.UbicacionId == u1.Id).Reservado);

        var factura = new DocumentoComercial
        {
            EmpresaId = empresa.Id, EsCompra = false, Tipo = TipoDocumento.Factura,
            NumeroDocumento = "FAC-1", Fecha = DateTime.Today, DocumentoOrigenId = albaran.Id,
            BaseImponible = 200m, TotalIva = 42m, Total = 242m,
            Lineas = new() { new DocumentoLinea { ArticuloId = art.Id, DescripcionArticulo = "A1", Cantidad = 10m, PrecioUnitario = 20m, PorcentajeIva = 21m } }
        };
        _context.Documentos.Add(factura);
        await _context.SaveChangesAsync();
        await stock.ProcesarMovimientoStock(factura.Id);

        det = await alsvc.GetDetalleAsync(empresa.Id, art.Id);
        Assert.Equal(40m, det.Huecos.First(h => h.UbicacionId == u1.Id).Cantidad);
        Assert.Equal(0m, det.Huecos.First(h => h.UbicacionId == u1.Id).Reservado);
        Assert.Equal(90m, (await _context.Articulos.FindAsync(art.Id))!.Stock);
    }

    [Fact]
    public async Task Picking_reparte_por_ruta_y_reposicion_detecta_minimos()
    {
        var (empresa, art) = await BaseAsync(stock: 30m, minimo: 50m);
        var alsvc = new AlmacenService(_context);
        var (_, u1, u2) = await NavesAsync(empresa.Id);
        await alsvc.UbicarStockAsync(empresa.Id, art.Id, u2.Id, 10m);
        await alsvc.UbicarStockAsync(empresa.Id, art.Id, u1.Id, 10m);

        var doc = new DocumentoComercial
        {
            EmpresaId = empresa.Id, EsCompra = false, Tipo = TipoDocumento.Pedido,
            NumeroDocumento = "PED-1", Fecha = DateTime.Today,
            Lineas = new() { new DocumentoLinea { ArticuloId = art.Id, DescripcionArticulo = "A1", Cantidad = 25m, PrecioUnitario = 20m, PorcentajeIva = 21m } }
        };
        _context.Documentos.Add(doc);
        await _context.SaveChangesAsync();

        var picking = await alsvc.GenerarPickingAsync(empresa.Id, doc.Id);
        var linea = picking.Lineas.Single();
        Assert.True(picking.Completo);
        Assert.Equal("A-01-01", linea.Origen.First().Origen.Split('/')[1]); // ruta: U1 primero
        Assert.Equal(25m, linea.Origen.Sum(o => o.Cantidad));

        var repo = await alsvc.GetReposicionAsync(empresa.Id);
        Assert.Single(repo);
        Assert.Equal(art.Id, repo[0].ArticuloId);
        Assert.True(repo[0].CantidadAReponer > 0);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
