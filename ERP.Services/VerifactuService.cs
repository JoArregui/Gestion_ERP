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
        var registro = new RegistroVerifactu
        {
            DocumentoId = factura.Id,
            EmpresaId = factura.EmpresaId,
            NifEmisor = empresa.CIF.Trim().ToUpperInvariant(),
            NumeroFactura = factura.NumeroDocumento.Trim(),
            FechaExpedicion = factura.Fecha.Date,
            TipoFactura = "F1",
            CuotaTotal = factura.TotalIva,
            ImporteTotal = factura.Total,
            FechaHoraHusoGeneracion = fechaGeneracion,
            EsPrimerRegistro = anterior is null,
            HuellaAnterior = anterior?.Huella,
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

    private static string FormatoImporte(decimal importe) => importe.ToString("0.##", CultureInfo.InvariantCulture);
}
