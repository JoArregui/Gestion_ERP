using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services;

public class VerifactuService
{
    private const string UrlCotejoProduccion = "https://www2.agenciatributaria.gob.es/wlpl/TIKE-CONT/ValidarQR";
    private readonly ApplicationDbContext _context;

    public VerifactuService(ApplicationDbContext context) => _context = context;

    public async Task<RegistroVerifactu> GenerarRegistroAltaAsync(DocumentoComercial factura)
    {
        if (factura.Tipo != TipoDocumento.Factura || factura.EsCompra)
            throw new InvalidOperationException("VERI*FACTU solo genera registros de alta para facturas expedidas.");

        var empresa = await _context.Empresas.FindAsync(factura.EmpresaId)
            ?? throw new InvalidOperationException("No existe la empresa emisora de la factura.");

        var anterior = await _context.RegistrosVerifactu
            .Where(r => r.EmpresaId == factura.EmpresaId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        var fechaGeneracion = DateTimeOffset.Now;
        var tipoFactura = factura.EsFacturaSimplificada ? "F2" : (factura.Tipo == TipoDocumento.FacturaRectificativa ? "R1" : "F1");
        var registro = new RegistroVerifactu
        {
            DocumentoId = factura.Id,
            EmpresaId = factura.EmpresaId,
            NifEmisor = empresa.CIF.Trim().ToUpperInvariant(),
            NombreRazonEmisor = empresa.RazonSocial,
            NumeroFactura = factura.NumeroDocumento.Trim(),
            FechaExpedicion = factura.Fecha.Date,
            TipoFactura = tipoFactura,
            TipoRectificativa = factura.TipoRectificativa,
            Incidencia = factura.IncidenciaVerifactu,
            FacturasRectificadasJson = factura.FacturasRectificadasJson,
            BaseImponible = factura.BaseImponible,
            CuotaTotal = factura.TotalIva,
            ImporteTotal = factura.Total,
            DescripcionOperacion = factura.Observaciones ?? factura.NotasInternas,
            FechaHoraHusoGeneracion = fechaGeneracion,
            EsPrimerRegistro = anterior is null,
            HuellaAnterior = anterior?.Huella,
            NombreSistemaInformatico = empresa.NombreSistemaInformatico ?? "ERP.NET",
            VersionSistemaInformatico = empresa.VersionSistemaInformatico ?? "1.0.0",
            IdSistemaInformatico = empresa.IdSistemaInformatico,
            NumeroInstalacion = empresa.NumeroInstalacion,
            EstadoRemision = "Pendiente"
        };

        registro.Huella = CalcularHuellaAlta(registro);
        registro.UrlQr = GenerarUrlQr(registro);
        registro.DatosRegistroJson = JsonSerializer.Serialize(new
        {
            registro.NifEmisor,
            registro.NumeroFactura,
            FechaExpedicionFactura = registro.FechaExpedicion.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            registro.TipoFactura,
            registro.CuotaTotal,
            registro.ImporteTotal,
            registro.HuellaAnterior,
            FechaHoraHusoGenRegistro = registro.FechaHoraHusoGeneracion.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            registro.Huella
        });

        _context.RegistrosVerifactu.Add(registro);
        await _context.SaveChangesAsync();
        return registro;
    }

    public static string CalcularHuellaAlta(RegistroVerifactu registro)
    {
        var datos = string.Join("&", new[]
        {
            $"IDEmisorFactura={registro.NifEmisor.Trim()}",
            $"NumSerieFactura={registro.NumeroFactura.Trim()}",
            $"FechaExpedicionFactura={registro.FechaExpedicion:dd-MM-yyyy}",
            $"TipoFactura={registro.TipoFactura.Trim()}",
            $"CuotaTotal={FormatoImporte(registro.CuotaTotal)}",
            $"ImporteTotal={FormatoImporte(registro.ImporteTotal)}",
            $"Huella={registro.HuellaAnterior?.Trim() ?? string.Empty}",
            $"FechaHoraHusoGenRegistro={registro.FechaHoraHusoGeneracion:yyyy-MM-ddTHH:mm:sszzz}"
        });

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(datos)));
    }

    public static string GenerarUrlQr(RegistroVerifactu registro) =>
        $"{UrlCotejoProduccion}?nif={Uri.EscapeDataString(registro.NifEmisor)}&numserie={Uri.EscapeDataString(registro.NumeroFactura)}&fecha={registro.FechaExpedicion:dd-MM-yyyy}&importe={FormatoImporte(registro.ImporteTotal)}";

    public async Task<RegistroVerifactuAnulacion> GenerarRegistroAnulacionAsync(int registroAltaId)
    {
        var alta = await _context.RegistrosVerifactu.FindAsync(registroAltaId)
            ?? throw new InvalidOperationException("Registro de alta no existe");
        var anterior = await _context.RegistrosVerifactu
            .Where(r => r.EmpresaId == alta.EmpresaId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
        // También considerar anulaciones previas como encadenamiento
        var ultimaAnulacion = await _context.RegistrosVerifactuAnulacion
            .Where(a => a.EmpresaId == alta.EmpresaId)
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();
        string? huellaPrev = ultimaAnulacion?.Huella ?? anterior?.Huella;

        var fechaGen = DateTimeOffset.Now;
        var anul = new RegistroVerifactuAnulacion
        {
            RegistroAltaId = alta.Id,
            EmpresaId = alta.EmpresaId,
            NifEmisor = alta.NifEmisor,
            NumeroFactura = alta.NumeroFactura,
            FechaExpedicion = alta.FechaExpedicion,
            FechaHoraHusoGeneracion = fechaGen,
            HuellaAnterior = huellaPrev,
            EstadoRemision = "Pendiente"
        };
        anul.Huella = CalcularHuellaAnulacion(anul);
        anul.DatosRegistroJson = JsonSerializer.Serialize(new
        {
            anul.NifEmisor,
            anul.NumeroFactura,
            FechaExpedicionFactura = anul.FechaExpedicion.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            anul.HuellaAnterior,
            FechaHoraHusoGenRegistro = anul.FechaHoraHusoGeneracion.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            anul.Huella
        });
        _context.RegistrosVerifactuAnulacion.Add(anul);
        await _context.SaveChangesAsync();
        return anul;
    }

    public static string CalcularHuellaAnulacion(RegistroVerifactuAnulacion r)
    {
        var datos = string.Join("&", new[]
        {
            $"IDEmisorFacturaAnulada={r.NifEmisor.Trim()}",
            $"NumSerieFacturaAnulada={r.NumeroFactura.Trim()}",
            $"FechaExpedicionFacturaAnulada={r.FechaExpedicion:dd-MM-yyyy}",
            $"Huella={r.HuellaAnterior?.Trim() ?? string.Empty}",
            $"FechaHoraHusoGenRegistro={r.FechaHoraHusoGeneracion:yyyy-MM-ddTHH:mm:sszzz}"
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(datos)));
    }

    private static string FormatoImporte(decimal importe) => importe.ToString("0.##", CultureInfo.InvariantCulture);
}
