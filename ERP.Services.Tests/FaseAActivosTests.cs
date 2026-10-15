using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Contabilidad;
using ERP.Services.Contabilidad;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Tests;

/// <summary>Fase A: activos fijos (plan lineal + asiento 681/281). No toca tests preexistentes.</summary>
public sealed class FaseAActivosTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public FaseAActivosTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CrearActivo_genera_plan_lineal_y_contabiliza_dotacion()
    {
        var empresa = new Empresa { NombreComercial = "T", RazonSocial = "T", CIF = "B12345678" };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        foreach (var (codigo, nombre, grupo) in new[] { ("21600", "Mobiliario", 2), ("28100", "Amort. acum. inmov. material", 2), ("68100", "Dotación amortización", 6) })
        {
            _context.CuentasContables.Add(new CuentaContable { Codigo = codigo, Nombre = nombre, Grupo = grupo, Nivel = 5, EsDetalle = true, EmpresaId = empresa.Id });
        }
        await _context.SaveChangesAsync();

        var svc = new ActivoFijoService(_context, new ContabilidadService(_context));
        var activo = await svc.CrearAsync(new ActivoFijo
        {
            EmpresaId = empresa.Id,
            Descripcion = "Portátil",
            CuentaActivoCodigo = "21600",
            CuentaAmortizacionCodigo = "28100",
            CuentaDotacionCodigo = "68100",
            PrecioAdquisicion = 1200m,
            VidaUtilAnios = 4,
            FechaAdquisicion = new DateTime(2026, 1, 15),
            FechaPuestaMarcha = new DateTime(2026, 1, 15)
        }, "test");

        Assert.Equal(4, activo.PlanAmortizacion.Count);
        Assert.Equal(300m, activo.PlanAmortizacion.OrderBy(l => l.Ejercicio).First().Dotacion);
        Assert.Equal(1200m, activo.PlanAmortizacion.Sum(l => l.Dotacion));

        var asiento = await svc.ContabilizarDotacionAsync(empresa.Id, activo.Id, 2026, "test");
        Assert.Equal(TipoAsiento.Amortizacion, asiento.Tipo);
        Assert.True(asiento.Cuadra);
        Assert.Contains(asiento.Apuntes, a => a.CuentaContableCodigo == "68100" && a.Tipo == TipoApunte.Debe && a.Importe == 300m);
        Assert.Contains(asiento.Apuntes, a => a.CuentaContableCodigo == "28100" && a.Tipo == TipoApunte.Haber && a.Importe == 300m);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
