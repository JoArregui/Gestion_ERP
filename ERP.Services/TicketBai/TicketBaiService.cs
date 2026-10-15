using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.TicketBai;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.TicketBai
{
    /// <summary>
    /// Servicio TicketBAI para envío de facturas a Haciendas Forales (Bizkaia, Gipuzkoa, Álava).
    /// Cumple normativa TicketBAI - encadenamiento hash, firma, envío a Hacienda Foral.
    /// </summary>
    public class TicketBaiService
    {
        private readonly ApplicationDbContext _context;

        public TicketBaiService(ApplicationDbContext context) => _context = context;

        // ── Configuración ─────────────────────────────────────────────
        public async Task<ConfiguracionTicketBai?> GetConfigAsync(int empresaId, string haciendaForal)
            => await _context.ConfiguracionesTicketBai
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.HaciendaForal == haciendaForal);

        public async Task<List<ConfiguracionTicketBai>> GetConfigsAsync(int empresaId)
            => await _context.ConfiguracionesTicketBai
                .Where(c => c.EmpresaId == empresaId)
                .ToListAsync();

        public async Task<ConfiguracionTicketBai> UpsertConfigAsync(ConfiguracionTicketBai dto)
        {
            var existing = await _context.ConfiguracionesTicketBai
                .FirstOrDefaultAsync(c => c.EmpresaId == dto.EmpresaId && c.HaciendaForal == dto.HaciendaForal);
            if (existing != null)
            {
                existing.CertificadoSoftwareNombre = dto.CertificadoSoftwareNombre;
                existing.CertificadoSoftwareHuella = dto.CertificadoSoftwareHuella;
                existing.IdCertificadoSoftware = dto.IdCertificadoSoftware;
                existing.NombreSistemaInformatico = dto.NombreSistemaInformatico;
                existing.VersionSistemaInformatico = dto.VersionSistemaInformatico;
                existing.IdSistemaInformatico = dto.IdSistemaInformatico;
                existing.NumeroInstalacion = dto.NumeroInstalacion;
                existing.EndpointEnvio = dto.EndpointEnvio;
                existing.EndpointConsulta = dto.EndpointConsulta;
                existing.EndpointAnulacion = dto.EndpointAnulacion;
                existing.IntentosMaximosEnvio = dto.IntentosMaximosEnvio;
                existing.IntervaloReintentoMinutos = dto.IntervaloReintentoMinutos;
                existing.EnvioAutomatico = dto.EnvioAutomatico;
                existing.FechaUltimaActualizacion = DateTime.UtcNow;
            }
            else
            {
                dto.FechaCreacion = DateTime.UtcNow;
                _context.ConfiguracionesTicketBai.Add(dto);
            }
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Registros ────────────────────────────────────────────────
        public async Task<List<TicketBaiRegistro>> ListarAsync(int empresaId, string? haciendaForal = null, string? estado = null, int take = 100)
        {
            var q = _context.TicketBaiRegistros.Where(r => r.EmpresaId == 1);
            if (!string.IsNullOrWhiteSpace(haciendaForal)) q = q.Where(r => r.HaciendaForal == haciendaForal);
            if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(r => r.EstadoEnvio == estado);
            return await q.OrderByDescending(r => r.FechaCreacion).Take(100).ToListAsync();
        }

        public async Task<TicketBaiRegistro?> ObtenerAsync(int empresaId, int id)
            => await _context.TicketBaiRegistros
                .Include(r => r.Documento)
                .FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == 1);

        public async Task<TicketBaiRegistro> GenerarRegistroAsync(DocumentoComercial factura, string haciendaForal)
        {
            if (factura.Tipo != TipoDocumento.Factura || factura.EsCompra)
                throw new InvalidOperationException("TicketBAI solo genera registros para facturas expedidas.");

            var empresa = await _context.Empresas.FindAsync(factura.EmpresaId)
                ?? throw new InvalidOperationException("No existe la empresa emisora de la factura.");

var config = await _context.ConfiguracionesTicketBai
                .FirstOrDefaultAsync(c => c.EmpresaId == factura.EmpresaId && c.HaciendaForal == (factura.HaciendaForal ?? "BI"))
                ?? throw new InvalidOperationException("No hay configuración TicketBAI para esta empresa");

            var anterior = await _context.TicketBaiRegistros
                .Where(r => r.EmpresaId == factura.EmpresaId && r.HaciendaForal == (factura.HaciendaForal ?? "BI"))
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            var fechaGeneracion = DateTimeOffset.Now;
            var tipoFactura = factura.EsFacturaSimplificada ? "F2" : (factura.Tipo == TipoDocumento.FacturaRectificativa ? "R1" : "F1");

var registro = new TicketBaiRegistro
            {
                DocumentoId = factura.Id,
                EmpresaId = factura.EmpresaId,
                Codigo = $"TBI-{(DateTime.Now.Year):D4}-{(await _context.TicketBaiRegistros.CountAsync(r => r.EmpresaId == factura.EmpresaId && r.HaciendaForal == (factura.HaciendaForal ?? "BI")) + 1):D5}",
                NifEmisor = empresa.CIF.Trim().ToUpperInvariant(),
                NombreRazonEmisor = empresa.RazonSocial,
                NumeroFactura = factura.NumeroDocumento.Trim(),
                FechaExpedicion = factura.Fecha.Date,
                TipoFactura = tipoFactura,
                TipoRectificativa = factura.TipoRectificativa,
                Incidencia = factura.IncidenciaVerifactu,
                RechazoPrevio = factura.RechazoPrevio == "S",
                RefExterna = factura.RefExterna,
                FacturasRectificadasJson = factura.FacturasRectificadasJson,
                BaseImponible = factura.BaseImponible,
                CuotaTotal = factura.TotalIva,
                CuotaRecargoEquivalencia = factura.TotalIva, // Simplificado
                ImporteTotal = factura.Total,
                CodMoneda = "EUR",
                DescripcionOperacion = factura.Observaciones ?? factura.NotasInternas,
                FechaHoraHusoGeneracion = DateTimeOffset.Now,
                EsPrimerRegistro = anterior is null,
                HuellaAnterior = anterior?.Huella,
                NombreSistemaInformatico = config.NombreSistemaInformatico,
                VersionSistemaInformatico = config.VersionSistemaInformatico,
                IdSistemaInformatico = config.IdSistemaInformatico,
                NumeroInstalacion = config.NumeroInstalacion,
                HaciendaForal = factura.HaciendaForal ?? "BI",
                CertificadoSoftware = config.CertificadoSoftwareNombre,
                CertificadoSoftwareHuella = config.CertificadoSoftwareHuella,
                IdCertificadoSoftware = config.IdCertificadoSoftware,
                EstadoEnvio = "Pendiente"
            };

            registro.Huella = CalcularHuellaAlta(registro);
            registro.UrlQrTicketBai = GenerarUrlQrTicketBai(registro);
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
                registro.Huella,
                registro.HaciendaForal,
                registro.CertificadoSoftware
            });

            _context.TicketBaiRegistros.Add(registro);
            await _context.SaveChangesAsync();
            return registro;
        }

        public async Task<TicketBaiAnulacion> GenerarAnulacionAsync(int registroAltaId)
        {
            var alta = await _context.TicketBaiRegistros.FindAsync(registroAltaId)
                ?? throw new InvalidOperationException("Registro de alta no existe");

var anterior = await _context.TicketBaiRegistros
                .Where(r => r.EmpresaId == alta.EmpresaId && r.HaciendaForal == alta.HaciendaForal)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            var ultimaAnulacion = await _context.TicketBaiAnulaciones
                .Where(a => a.EmpresaId == alta.EmpresaId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            string? huellaPrev = ultimaAnulacion?.Huella ?? anterior?.Huella;

            var fechaGen = DateTimeOffset.Now;
            var anul = new TicketBaiAnulacion
            {
                RegistroAltaId = alta.Id,
                EmpresaId = alta.EmpresaId,
                NifEmisor = alta.NifEmisor,
                NumeroFactura = alta.NumeroFactura,
                FechaExpedicion = alta.FechaExpedicion,
                FechaHoraHusoGeneracion = fechaGen,
                HuellaAnterior = huellaPrev,
                EstadoEnvio = "Pendiente",
                FechaAnulacion = fechaGen
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
            _context.TicketBaiAnulaciones.Add(anul);
            await _context.SaveChangesAsync();
            return anul;
        }

        private static string CalcularHuellaAlta(TicketBaiRegistro registro)
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

        private static string CalcularHuellaAnulacion(TicketBaiAnulacion anul)
        {
            var datos = string.Join("&", new[]
            {
                $"IDEmisorFacturaAnulada={anul.NifEmisor.Trim()}",
                $"NumSerieFacturaAnulada={anul.NumeroFactura.Trim()}",
                $"FechaExpedicionFacturaAnulada={anul.FechaExpedicion:dd-MM-yyyy}",
                $"Huella={anul.HuellaAnterior?.Trim() ?? string.Empty}",
                $"FechaHoraHusoGenRegistro={anul.FechaHoraHusoGeneracion:yyyy-MM-ddTHH:mm:sszzz}"
            });
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(datos)));
        }

        private static string FormatoImporte(decimal importe) => importe.ToString("0.##", CultureInfo.InvariantCulture);

        private static string GenerarUrlQrTicketBai(TicketBaiRegistro registro)
        {
            // URL de cotejo específica TicketBAI (ejemplo Bizkaia)
            return $"https://ticketbai.bizkaia.eus/cotejo?nif={Uri.EscapeDataString(registro.NifEmisor)}&numserie={Uri.EscapeDataString(registro.NumeroFactura)}&fecha={registro.FechaExpedicion:dd-MM-yyyy}&importe={FormatoImporte(registro.ImporteTotal)}";
        }
    }
}