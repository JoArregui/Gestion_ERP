using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.CRM;
using ERP.Services;
using ERP.Services.CRM;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Tests;

/// <summary>Fase B: leads → cliente + embudo → presupuesto.</summary>
public sealed class FaseBCrmTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public FaseBCrmTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private CrmService CrearCrm()
    {
        var stock = new StockService(_context);
        var ciclo = new CicloFacturacionService(_context, new FacturacionService(_context, stock, new VerifactuService(_context)), stock);
        return new CrmService(_context, ciclo);
    }

    [Fact]
    public async Task Flujo_completo_lead_a_presupuesto()
    {
        var empresa = new Empresa { NombreComercial = "T", RazonSocial = "T", CIF = "B12345678" };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        var crm = CrearCrm();

        var lead = await crm.CrearLeadAsync(new Lead { EmpresaId = empresa.Id, Nombre = "Ana", EmpresaNombre = "Acme", Email = "a@acme.es" }, "test");
        Assert.Equal(EstadoLead.Nuevo, lead.Estado);

        var (cliente, op0) = await crm.ConvertirLeadAsync(empresa.Id, lead.Id, "test");
        Assert.Equal("CLI-0001", cliente.CodigoCliente);
        Assert.Equal(EstadoLead.Convertido, (await _context.Leads.FindAsync(lead.Id))!.Estado);
        Assert.Equal(EtapaOportunidad.Prospeccion, op0.Etapa);

        await crm.CambiarEtapaAsync(empresa.Id, op0.Id, EtapaOportunidad.Calificacion);
        await crm.CambiarEtapaAsync(empresa.Id, op0.Id, EtapaOportunidad.Propuesta);
        op0.ImporteEstimado = 1000m;
        await _context.SaveChangesAsync();
        await crm.CambiarEtapaAsync(empresa.Id, op0.Id, EtapaOportunidad.Negociacion);

        var doc = await crm.ConvertirAPresupuestoAsync(empresa.Id, op0.Id);
        Assert.Equal(TipoDocumento.Presupuesto, doc.Tipo);
        Assert.Equal(1210m, doc.Total); // 1000 + 21% IVA
    }

    [Fact]
    public async Task Oportunidad_con_importe_genera_presupuesto_correcto_y_pipeline()
    {
        var empresa = new Empresa { NombreComercial = "T", RazonSocial = "T", CIF = "B87654321" };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        var crm = CrearCrm();

        var op = await crm.CrearOportunidadAsync(new Oportunidad { EmpresaId = empresa.Id, Titulo = "Venta 1000", ImporteEstimado = 1000m, Probabilidad = 50 }, "test");
        await crm.CambiarEtapaAsync(empresa.Id, op.Id, EtapaOportunidad.Calificacion);
        await crm.CambiarEtapaAsync(empresa.Id, op.Id, EtapaOportunidad.Propuesta);
        await crm.CambiarEtapaAsync(empresa.Id, op.Id, EtapaOportunidad.Negociacion);
        var doc = await crm.ConvertirAPresupuestoAsync(empresa.Id, op.Id);
        Assert.Equal(1210m, doc.Total);

        var op2 = await crm.CrearOportunidadAsync(new Oportunidad { EmpresaId = empresa.Id, Titulo = "Venta 2000", ImporteEstimado = 2000m, Probabilidad = 25 }, "test");
        var pipe = await crm.GetPipelineAsync(empresa.Id);
        // op ganada (1000) + op2 prospección (2000)
        Assert.Equal(2, pipe.Total);
        Assert.Equal(3000m, pipe.ImporteTotal);
    }

    [Fact]
    public async Task Etapa_rechaza_saltos_y_exige_motivo_en_perdida()
    {
        var empresa = new Empresa { NombreComercial = "T", RazonSocial = "T", CIF = "B11111111" };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        var crm = CrearCrm();

        var op = await crm.CrearOportunidadAsync(new Oportunidad { EmpresaId = empresa.Id, Titulo = "X", ImporteEstimado = 100m }, "test");
        await Assert.ThrowsAsync<InvalidOperationException>(() => crm.CambiarEtapaAsync(empresa.Id, op.Id, EtapaOportunidad.Ganada));
        await Assert.ThrowsAsync<ArgumentException>(() => crm.CambiarEtapaAsync(empresa.Id, op.Id, EtapaOportunidad.Perdida));
        await crm.CambiarEtapaAsync(empresa.Id, op.Id, EtapaOportunidad.Perdida, "Precio");
        Assert.Equal(EtapaOportunidad.Perdida, (await _context.Oportunidades.FindAsync(op.Id))!.Etapa);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
