using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Tests;

public sealed class TenantIsolationTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TenantContextStub _tenant = new();
    private readonly ApplicationDbContext _context;

    public TenantIsolationTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options, _tenant);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Las_consultas_de_entidades_tenant_aislan_por_empresa_activa()
    {
        var empresaA = await CrearEmpresaAsync("Empresa A");
        var empresaB = await CrearEmpresaAsync("Empresa B");
        _context.Clientes.AddRange(
            new Cliente { EmpresaId = empresaA.Id, CodigoCliente = "A-1", RazonSocial = "Cliente A", CIF = "B12345678" },
            new Cliente { EmpresaId = empresaB.Id, CodigoCliente = "B-1", RazonSocial = "Cliente B", CIF = "B87654321" });
        await _context.SaveChangesAsync();

        _tenant.EmpresaId = empresaA.Id;
        var clientesA = await _context.Clientes.AsNoTracking().ToListAsync();
        _tenant.EmpresaId = empresaB.Id;
        var clientesB = await _context.Clientes.AsNoTracking().ToListAsync();

        Assert.Single(clientesA);
        Assert.Equal(empresaA.Id, clientesA[0].EmpresaId);
        Assert.Single(clientesB);
        Assert.Equal(empresaB.Id, clientesB[0].EmpresaId);
    }

    private async Task<Empresa> CrearEmpresaAsync(string nombre)
    {
        var empresa = new Empresa
        {
            NombreComercial = nombre,
            RazonSocial = nombre,
            CIF = $"B{Guid.NewGuid():N}"[..10]
        };
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        return empresa;
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class TenantContextStub : ITenantContext
    {
        public int? EmpresaId { get; set; }
    }
}
