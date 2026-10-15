using ERP.Data;
using ERP.Domain.DTOs;
using ERP.Domain.Entities;
using ERP.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Tests;

/// <summary>Fase C: homologación, RFQ/comparativa/adjudicación y 3-way matching.</summary>
public sealed class FaseCComprasTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public FaseCComprasTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private async Task<(Empresa empresa, Familia familia)> BaseAsync()
    {
        var empresa = new Empresa { NombreComercial = "T", RazonSocial = "T", CIF = "B12345678" };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        var familia = new Familia { Nombre = "F", EmpresaId = empresa.Id };
        _context.Familia.Add(familia);
        await _context.SaveChangesAsync();
        return (empresa, familia);
    }

    private async Task<Articulo> ArticuloAsync(int empresaId, int familiaId, string codigo, decimal stock = 10m)
    {
        var a = new Articulo { EmpresaId = empresaId, FamiliaId = familiaId, Codigo = codigo, Descripcion = codigo, Stock = stock, PrecioCompra = 10m, PrecioVenta = 20m, PorcentajeIva = 21m };
        _context.Articulos.Add(a);
        await _context.SaveChangesAsync();
        return a;
    }

    private async Task<Proveedor> ProveedorAsync(int empresaId, string nombre, bool homologado = true)
    {
        var p = new Proveedor { EmpresaId = empresaId, RazonSocial = nombre, CIF = $"B{Guid.NewGuid():N}"[..10], Homologado = homologado };
        _context.Proveedores.Add(p);
        await _context.SaveChangesAsync();
        return p;
    }

    [Fact]
    public async Task Pedido_exige_homologacion_y_respeta_bloqueo()
    {
        var (empresa, familia) = await BaseAsync();
        var art = await ArticuloAsync(empresa.Id, familia.Id, "A1");
        var svc = new ComprasService(_context);

        var nuevo = await ProveedorAsync(empresa.Id, "Nuevo", homologado: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CrearPedidoAsync(new PedidoCompraRequest
            {
                ProveedorId = nuevo.Id,
                Lineas = new() { new LineaPedidoCompraRequest { ArticuloId = art.Id, Cantidad = 1, PrecioUnitario = 5 } }
            }, empresa.Id));

        await svc.HomologarProveedorAsync(empresa.Id, nuevo.Id, "ISO9001", "test");
        var pedido = await svc.CrearPedidoAsync(new PedidoCompraRequest
        {
            ProveedorId = nuevo.Id,
            Lineas = new() { new LineaPedidoCompraRequest { ArticuloId = art.Id, Cantidad = 2, PrecioUnitario = 5 } }
        }, empresa.Id);
        Assert.Equal(TipoDocumento.Pedido, pedido.Tipo);
        Assert.True(pedido.EsCompra);

        await svc.BloquearProveedorAsync(empresa.Id, nuevo.Id, "Impago");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CrearPedidoAsync(new PedidoCompraRequest
            {
                ProveedorId = nuevo.Id,
                Lineas = new() { new LineaPedidoCompraRequest { ArticuloId = art.Id, Cantidad = 1, PrecioUnitario = 5 } }
            }, empresa.Id));
    }

    [Fact]
    public async Task Rfq_comparativa_adjudica_mejor_oferta_y_crea_pedido()
    {
        var (empresa, familia) = await BaseAsync();
        var a1 = await ArticuloAsync(empresa.Id, familia.Id, "A1");
        var a2 = await ArticuloAsync(empresa.Id, familia.Id, "A2");
        var pa = await ProveedorAsync(empresa.Id, "Proveedor A");
        var pb = await ProveedorAsync(empresa.Id, "Proveedor B");
        var svc = new ComprasService(_context);

        var rfq = await svc.CrearRFQAsync(empresa.Id, new() { (a1.Id, 10m), (a2.Id, 5m) }, null, "Test", "test");
        Assert.Equal("RFQ-0001", rfq.Codigo);
        await svc.EnviarRFQAsync(empresa.Id, rfq.Id, new() { pa.Id, pb.Id });

        await svc.RegistrarOfertaAsync(empresa.Id, rfq.Id, pa.Id,
            new() { (a1.Id, 10m, 9m), (a2.Id, 5m, 20m) }, 7, null);
        var ofertaB = await svc.RegistrarOfertaAsync(empresa.Id, rfq.Id, pb.Id,
            new() { (a1.Id, 10m, 8m), (a2.Id, 5m, 19m) }, 5, null);

        var comp = await svc.GetComparativaAsync(empresa.Id, rfq.Id);
        Assert.Equal(2, comp.Totales.Count);
        Assert.Equal(ofertaB.Id, comp.Totales.OrderBy(t => t.Total).First().OfertaId); // B más barata
        var lineaA1 = comp.Lineas.First(l => l.ArticuloId == a1.Id);
        Assert.Equal(8m, lineaA1.Precios.First().Precio); // mejor precio A1 = B

        var pedido = await svc.AdjudicarAsync(empresa.Id, rfq.Id, ofertaB.Id);
        Assert.Equal(pa.Id == pedido.ProveedorId ? pa.Id : pb.Id, pedido.ProveedorId);
        Assert.Equal(pb.Id, pedido.ProveedorId);
        Assert.Equal(10m * 8m + 5m * 19m, pedido.BaseImponible);
        Assert.Equal(ERP.Domain.Entities.Compras.EstadoRFQ.Adjudicada,
            (await _context.SolicitudesOferta.FindAsync(rfq.Id))!.Estado);
    }

    [Fact]
    public async Task Recepcion_3way_rechaza_descuadre_y_acepta_tolerancia()
    {
        var (empresa, familia) = await BaseAsync();
        var art = await ArticuloAsync(empresa.Id, familia.Id, "A1", stock: 10m);
        var prov = await ProveedorAsync(empresa.Id, "Prov");
        var svc = new ComprasService(_context);

        var pedido = await svc.CrearPedidoAsync(new PedidoCompraRequest
        {
            ProveedorId = prov.Id,
            Lineas = new() { new LineaPedidoCompraRequest { ArticuloId = art.Id, Cantidad = 10, PrecioUnitario = 10 } }
        }, empresa.Id);

        var (okMal, errMal) = await svc.RecepcionarConFacturaAsync(pedido.Id, "ALB-1", "F-1", pedido.Total * 1.5m);
        Assert.False(okMal);
        Assert.Contains("Descuadre 3-way", errMal);

        var (ok, _) = await svc.RecepcionarConFacturaAsync(pedido.Id, "ALB-1", "F-1", pedido.Total);
        Assert.True(ok);
        Assert.Equal("F-1", (await _context.Documentos.FindAsync(pedido.Id))!.NumeroFacturaProveedor);
        Assert.Equal(20m, (await _context.Articulos.FindAsync(art.Id))!.Stock);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
