using System.Security.Claims;
using ERP.API.Controllers;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Tests;

public sealed class CierreCajaSecurityTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public CierreCajaSecurityTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void CierreCajaController_requiere_autenticacion()
    {
        var authorize = typeof(CierreCajaController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(authorize);
    }

    [Fact]
    public async Task GetHistorial_rechaza_otra_empresa()
    {
        var empresaAutorizada = await CrearEmpresaAsync("Autorizada");
        var otraEmpresa = await CrearEmpresaAsync("Otra");
        _context.CierresCaja.Add(new CierreCaja { EmpresaId = otraEmpresa.Id });
        await _context.SaveChangesAsync();
        var controller = CrearController(empresaAutorizada.Id);

        var result = await controller.GetHistorial(otraEmpresa.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task EjecutarCierre_rechaza_cuerpo_de_otra_empresa_y_no_guarda_datos()
    {
        var empresaAutorizada = await CrearEmpresaAsync("Autorizada");
        var otraEmpresa = await CrearEmpresaAsync("Otra");
        var controller = CrearController(empresaAutorizada.Id);

        var result = await controller.EjecutarCierre(new CierreCaja { EmpresaId = otraEmpresa.Id });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(await _context.CierresCaja.ToListAsync());
    }

    [Fact]
    public async Task DescargarCierrePdf_no_expone_un_cierre_de_otra_empresa()
    {
        var empresaAutorizada = await CrearEmpresaAsync("Autorizada");
        var otraEmpresa = await CrearEmpresaAsync("Otra");
        var cierre = new CierreCaja { EmpresaId = otraEmpresa.Id };
        _context.CierresCaja.Add(cierre);
        await _context.SaveChangesAsync();
        var controller = CrearController(empresaAutorizada.Id);

        var result = await controller.DescargarCierrePdf(cierre.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    private CierreCajaController CrearController(int empresaId)
    {
        var controller = new CierreCajaController(_context, new PdfService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim("EmpresaId", empresaId.ToString()) },
                        authenticationType: "Test"))
                }
            }
        };
        return controller;
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
}
