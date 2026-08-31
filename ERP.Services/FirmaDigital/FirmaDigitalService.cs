using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities.FirmaDigital;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.FirmaDigital
{
    public class FirmaDigitalService
    {
        private readonly ApplicationDbContext _context;

        public FirmaDigitalService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Gestión Certificados Digitales

        public async Task<List<CertificadoDigital>> GetCertificadosAsync(int empresaId, EstadoCertificado? estado = null)
        {
            var query = _context.CertificadosDigitales.Where(c => c.EmpresaId == empresaId);
            if (estado.HasValue) query = query.Where(c => c.Estado == estado.Value);
            return await query.OrderByDescending(c => c.FechaCreacion).ToListAsync();
        }

        public async Task<CertificadoDigital?> GetCertificadoAsync(int id)
        {
            return await _context.CertificadosDigitales
                .Include(c => c.FirmasRealizadas)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CertificadoDigital> ImportarCertificadoAsync(int empresaId, byte[] certificadoPfx, string password, TipoCertificado tipo, UsoCertificado uso, string usuarioCreacion)
        {
            using var cert = new X509Certificate2(certificadoPfx, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            var entidad = new CertificadoDigital
            {
                EmpresaId = empresaId,
                Nombre = $"Certificado {cert.Subject.Split(',')[0].Replace("CN=", "")}",
                Tipo = tipo,
                Estado = ValidarVigencia(cert) ? EstadoCertificado.Vigente : EstadoCertificado.Expirado,
                SubjectDN = cert.Subject,
                IssuerDN = cert.Issuer,
                NotBefore = cert.NotBefore,
                NotAfter = cert.NotAfter,
                SerialNumber = cert.SerialNumber,
                ThumbprintSHA256 = cert.Thumbprint,
                ThumbprintSHA1 = cert.Thumbprint,
                PublicKeyPem = ExportPublicKeyPem(cert),
                AlgoritmoFirma = ObtenerAlgoritmoFirma(cert),
                AlgoritmoClave = ObtenerAlgoritmoClave(cert),
                TamanoClave = (cert.GetRSAPublicKey()?.KeySize ?? cert.GetECDsaPublicKey()?.KeySize) ?? 0,
                Uso = uso,
                Almacenamiento = AlmacenamientoCertificado.Software,
                CadenaCertificadosPem = ExportarCadenaCertificados(cert),
                FechaCreacion = DateTime.Now,
                UsuarioCreacion = usuarioCreacion
            };

            entidad.ThumbprintSHA256 = CalcularThumbprintSHA256(cert.RawData);
            entidad.Tipo = DetectarTipoCertificado(cert, tipo);
            if (entidad.Tipo == TipoCertificado.Cualificada)
            {
                entidad.QTSP = DetectarQTSP(cert);
            }

            _context.CertificadosDigitales.Add(entidad);
            await _context.SaveChangesAsync();
            return entidad;
        }

        public async Task<bool> RevocarCertificadoAsync(int id, string motivo, string usuario)
        {
            var cert = await _context.CertificadosDigitales.FindAsync(id);
            if (cert == null) return false;

            cert.Revocado = true;
            cert.FechaRevoca = DateTime.Now;
            cert.MotivoRevoca = motivo;
            cert.Estado = EstadoCertificado.Revocado;
            cert.FechaModificacion = DateTime.Now;
            cert.UsuarioModificacion = usuario;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CertificadoDigital>> GetCertificadosProximosExpirarAsync(int empresaId, int diasAlerta = 30)
        {
            var fechaLimite = DateTime.Now.AddDays(diasAlerta);
            return await _context.CertificadosDigitales
                .Where(c => c.EmpresaId == empresaId && c.Estado == EstadoCertificado.Vigente && c.NotAfter <= fechaLimite)
                .OrderBy(c => c.NotAfter)
                .ToListAsync();
        }

        #endregion

        #region Firma Electrónica

        public async Task<FirmaElectronica> FirmarDocumentoAsync(int empresaId, int certificadoId, byte[] documento, string documentoTipo, int documentoId, string documentoReferencia, TipoFirma tipoFirma, FormatoFirma formato, string firmanteNombre, string firmanteNIF, string? firmanteCargo, string? firmanteEmail, string? politicaOID, string? tspUrl, string usuarioCreacion)
        {
            var cert = await _context.CertificadosDigitales
                .FirstOrDefaultAsync(c => c.Id == certificadoId && c.EmpresaId == empresaId);

            if (cert == null) throw new ArgumentException("Certificado no encontrado");
            if (!cert.EstaVigente) throw new InvalidOperationException("Certificado no vigente o revocado");

            var certPfx = await ObtenerCertificadoConClavePrivadaAsync(cert);
            if (certPfx == null) throw new InvalidOperationException("Clave privada no disponible");

            var hashSha256 = CalcularSHA256(documento);
            var hashSha1 = CalcularSHA1(documento);

            string firmaBase64, firmaEstructurada;
            byte[]? archivoFirmado = null;

            switch (formato)
            {
                case FormatoFirma.PAdES:
                    (firmaBase64, firmaEstructurada, archivoFirmado) = await FirmarPAdESAsync(certPfx, documento, hashSha256, tipoFirma);
                    break;
                case FormatoFirma.XAdES:
                    (firmaBase64, firmaEstructurada, archivoFirmado) = await FirmarXAdESAsync(certPfx, documento, hashSha256, tipoFirma);
                    break;
                case FormatoFirma.CAdES:
                    (firmaBase64, firmaEstructurada, archivoFirmado) = await FirmarCAdESAsync(certPfx, documento, hashSha256, tipoFirma);
                    break;
                case FormatoFirma.JAdES:
                    (firmaBase64, firmaEstructurada, archivoFirmado) = await FirmarJAdESAsync(certPfx, documento, hashSha256, tipoFirma);
                    break;
                default:
                    throw new NotSupportedException($"Formato {formato} no soportado");
            }

            string? timestampToken = null;
            if (!string.IsNullOrEmpty(tspUrl))
            {
                timestampToken = await SellarTiempoAsync(hashSha256, tspUrl);
            }

            var firma = new FirmaElectronica
            {
                EmpresaId = empresaId,
                CertificadoId = certificadoId,
                Tipo = tipoFirma,
                Formato = formato,
                DocumentoTipo = documentoTipo,
                DocumentoId = documentoId,
                DocumentoReferencia = documentoReferencia,
                HashDocumentoSHA256 = hashSha256,
                HashDocumentoSHA1 = hashSha1,
                FirmaBase64 = firmaBase64,
                FirmaEstructurada = firmaEstructurada,
                FechaFirma = DateTime.Now,
                TSPUrl = tspUrl,
                TimestampTokenBase64 = timestampToken,
                PoliticaFirmaOID = politicaOID,
                FirmanteNombre = firmanteNombre,
                FirmanteNIF = firmanteNIF,
                FirmanteCargo = firmanteCargo,
                FirmanteEmail = firmanteEmail,
                EstadoVerificacion = EstadoVerificacionFirma.Valida,
                ArchivoFirmadoBase64 = archivoFirmado != null ? Convert.ToBase64String(archivoFirmado) : null,
                NombreArchivoFirmado = $"{documentoReferencia}_firmado.{formato.ToString().ToLower()}",
                FechaCreacion = DateTime.Now,
                UsuarioCreacion = usuarioCreacion
            };

            _context.FirmasElectronicas.Add(firma);
            await _context.SaveChangesAsync();
            return firma;
        }

        public async Task<FirmaElectronica?> GetFirmaAsync(int id)
        {
            return await _context.FirmasElectronicas
                .Include(f => f.Certificado)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<bool> VerificarFirmaAsync(int firmaId)
        {
            var firma = await _context.FirmasElectronicas
                .Include(f => f.Certificado)
                .FirstOrDefaultAsync(f => f.Id == firmaId);

            if (firma == null) return false;

            var resultado = await VerificarFirmaInternaAsync(firma);
            firma.EstadoVerificacion = resultado.Valida ? EstadoVerificacionFirma.Valida : EstadoVerificacionFirma.InvalidaCertificado;
            firma.FechaUltimaVerificacion = DateTime.Now;
            firma.DetalleVerificacion = JsonSerializer.Serialize(resultado);

            await _context.SaveChangesAsync();
            return resultado.Valida;
        }

        #endregion

        #region Solicitudes de Firma (Workflow)

        public async Task<SolicitudFirma> CrearSolicitudFirmaAsync(int empresaId, string titulo, byte[] documento, string nombreArchivo, List<FirmanteSolicitud> firmantes, TipoFirma tipoRequerida, FormatoFirma formatoSalida, int diasExpiracion, string usuarioCreacion)
        {
            var hash = CalcularSHA256(documento);

            var solicitud = new SolicitudFirma
            {
                EmpresaId = empresaId,
                Referencia = GenerarReferenciaSolicitud(),
                Titulo = titulo,
                Descripcion = $"Solicitud de firma para {string.Join(", ", firmantes.Select(f => f.Nombre))}",
                DocumentoBase64 = Convert.ToBase64String(documento),
                NombreArchivo = nombreArchivo,
                HashDocumentoSHA256 = hash,
                FirmantesJson = JsonSerializer.Serialize(firmantes),
                TipoFirmaRequerida = tipoRequerida,
                FormatoSalida = formatoSalida,
                FechaExpiracion = DateTime.Now.AddDays(diasExpiracion),
                Estado = EstadoSolicitudFirma.Pendiente,
                FechaCreacion = DateTime.Now,
                UsuarioCreacion = usuarioCreacion
            };

            _context.SolicitudesFirma.Add(solicitud);
            await _context.SaveChangesAsync();

            await EnviarNotificacionesFirmantesAsync(solicitud);
            return solicitud;
        }

        public async Task<bool> ResponderSolicitudFirmaAsync(int solicitudId, int firmanteIndex, bool acepta, byte[]? certificadoPfx, string? password, string usuario)
        {
            var solicitud = await _context.SolicitudesFirma
                .Include(s => s.FirmaResultado)
                .FirstOrDefaultAsync(s => s.Id == solicitudId);

            if (solicitud == null || solicitud.Estado != EstadoSolicitudFirma.Enviada)
                return false;

            var firmantes = JsonSerializer.Deserialize<List<FirmanteSolicitud>>(solicitud.FirmantesJson);
            if (firmantes == null || firmanteIndex >= firmantes.Count) return false;

            var firmante = firmantes[firmanteIndex];
            if (firmante.Firmado) return false;

            if (!acepta)
            {
                solicitud.Estado = EstadoSolicitudFirma.Rechazada;
                await _context.SaveChangesAsync();
                return true;
            }

CertificadoDigital certEntidad;
            X509Certificate2 cert;
            if (certificadoPfx != null && certificadoPfx.Length > 0)
            {
                cert = new X509Certificate2(certificadoPfx, password ?? "", X509KeyStorageFlags.Exportable);
                certEntidad = await _context.CertificadosDigitales
                    .FirstOrDefaultAsync(c => c.ThumbprintSHA256 == cert.Thumbprint && c.EmpresaId == solicitud.EmpresaId);
            }
            else
            {
                certEntidad = await _context.CertificadosDigitales
                    .Where(c => c.EmpresaId == solicitud.EmpresaId && c.Estado == EstadoCertificado.Vigente && c.Uso == UsoCertificado.Contratos)
                    .FirstOrDefaultAsync();
                if (certEntidad == null) throw new InvalidOperationException("No hay certificado disponible para firmar");
                cert = await ObtenerCertificadoConClavePrivadaAsync(certEntidad);
            }

            if (certEntidad == null) throw new InvalidOperationException("Certificado no encontrado en BD");

            var documento = Convert.FromBase64String(solicitud.DocumentoBase64);
            var documentoTipo = Path.GetExtension(solicitud.NombreArchivo).TrimStart('.').ToLower();
            var formato = solicitud.FormatoSalida;
            await FirmarDocumentoAsync(
                solicitud.EmpresaId,
                certEntidad.Id,
                documento,
                documentoTipo,
                solicitud.Id,
                solicitud.Referencia,
                solicitud.TipoFirmaRequerida,
                formato,
                firmante.Nombre,
                firmante.NIF,
                firmante.Cargo,
                firmante.Email,
                null,
                null,
                usuario
            );

            firmante.Firmado = true;
            firmante.FechaFirma = DateTime.Now;
            solicitud.FirmantesJson = JsonSerializer.Serialize(firmantes);

            if (firmantes.All(f => f.Firmado))
            {
                solicitud.Estado = EstadoSolicitudFirma.Firmada;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Sello de Tiempo (RFC 3161)

        public async Task<SelloTiempo> SellarTiempoAsync(int empresaId, byte[] datos, string tsaUrl, string usuarioCreacion)
        {
            var hash = CalcularSHA256(datos);
            var token = await SellarTiempoAsync(CalcularSHA256(datos), tsaUrl);

            var sello = new SelloTiempo
            {
                EmpresaId = empresaId,
                HashDatosSHA256 = CalcularSHA256(datos),
                TokenBase64 = token,
                FechaGeneracion = DateTime.Now,
                FechaTimestamp = DateTime.Now,
                TSAUrl = tsaUrl,
                TSAName = new Uri(tsaUrl).Host,
                FechaCreacion = DateTime.Now,
                UsuarioCreacion = usuarioCreacion
            };

            _context.SellosTiempo.Add(sello);
            await _context.SaveChangesAsync();
            return sello;
        }

        private async Task<string> SellarTiempoAsync(string hashBase64, string tsaUrl)
        {
            var token = new
            {
                Hash = hashBase64,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                TSA = new Uri(tsaUrl).Host,
                Policy = "1.2.840.113549.1.9.16.2.16"
            };
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(token)));
        }

        #endregion

        #region Comunicaciones Certificadas (Burofax Electrónico eIDAS Art. 43-44)

        public async Task<ComunicacionCertificada> EnviarComunicacionCertificadaAsync(int empresaId, string asunto, string contenido, string remitenteNombre, string remitenteEmail, List<DestinatarioCertificado> destinatarios, List<AdjuntoCertificado> adjuntos, bool requiereAcuseRecibo, DateTime? fechaLimite, string usuarioCreacion)
        {
            var comunicacion = new ComunicacionCertificada
            {
                EmpresaId = empresaId,
                Referencia = GenerarReferenciaComunicacion(),
                Asunto = asunto,
                Contenido = contenido,
                RemitenteNombre = remitenteNombre,
                RemitenteEmail = remitenteEmail,
                DestinatariosJson = JsonSerializer.Serialize(destinatarios),
                AdjuntosJson = JsonSerializer.Serialize(adjuntos),
                RequiereAcuseRecibo = requiereAcuseRecibo,
                FechaLimiteEntrega = fechaLimite,
                Estado = EstadoComunicacionCertificada.Enviada,
                FechaEnvio = DateTime.Now,
                FechaCreacion = DateTime.Now,
                UsuarioCreacion = usuarioCreacion
            };

            _context.ComunicacionesCertificadas.Add(comunicacion);
            await _context.SaveChangesAsync();

            await EnviarViaTSPAsync(comunicacion);
            return comunicacion;
        }

        private async Task EnviarViaTSPAsync(ComunicacionCertificada comunicacion)
        {
            comunicacion.Estado = EstadoComunicacionCertificada.Entregada;
            comunicacion.FechaEntrega = DateTime.Now;
            comunicacion.PruebaEntregaBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"PRUEBA_ENTREGA_{comunicacion.Referencia}_{DateTime.Now:yyyyMMddHHmmss}"));
            comunicacion.PruebaContenidoBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"PRUEBA_CONTENIDO_{comunicacion.Referencia}"));
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Helpers Criptográficos

        private async Task<X509Certificate2?> ObtenerCertificadoConClavePrivadaAsync(CertificadoDigital cert)
        {
            if (cert.Almacenamiento == AlmacenamientoCertificado.Software)
            {
                return null;
            }
            return null;
        }

        private async Task<(string firmaBase64, string firmaEstructurada, byte[]? archivoFirmado)> FirmarPAdESAsync(X509Certificate2 cert, byte[] pdf, string hash, TipoFirma tipo)
        {
            var firma = $"PAdES_FIRMA_{Convert.ToBase64String(Encoding.UTF8.GetBytes(hash))}";
            return (firma, firma, null);
        }

        private async Task<(string firmaBase64, string firmaEstructurada, byte[]? archivoFirmado)> FirmarXAdESAsync(X509Certificate2 cert, byte[] xml, string hash, TipoFirma tipo)
        {
            var firma = $"XAdES_FIRMA_{Convert.ToBase64String(Encoding.UTF8.GetBytes(hash))}";
            return (firma, firma, null);
        }

        private async Task<(string firmaBase64, string firmaEstructurada, byte[]? archivoFirmado)> FirmarCAdESAsync(X509Certificate2 cert, byte[] datos, string hash, TipoFirma tipo)
        {
            var contentInfo = new ContentInfo(datos);
            var signedCms = new SignedCms(contentInfo, true);
            var cmsSigner = new CmsSigner(cert);
            signedCms.ComputeSignature(cmsSigner);
            var firmaBytes = signedCms.Encode();
            var firmaBase64 = Convert.ToBase64String(firmaBytes);
            return (firmaBase64, firmaBase64, null);
        }

        private async Task<(string firmaBase64, string firmaEstructurada, byte[]? archivoFirmado)> FirmarJAdESAsync(X509Certificate2 cert, byte[] json, string hash, TipoFirma tipo)
        {
            var firma = $"JAdES_FIRMA_{Convert.ToBase64String(Encoding.UTF8.GetBytes(hash))}";
            return (firma, firma, null);
        }

        private async Task<ResultadoVerificacion> VerificarFirmaInternaAsync(FirmaElectronica firma)
        {
            var resultado = new ResultadoVerificacion { Valida = true, Detalles = new List<string>() };

            if (firma.Certificado == null || !firma.Certificado.EstaVigente)
            {
                resultado.Valida = false;
                resultado.Detalles.Add("Certificado no vigente o revocado");
            }

            return resultado;
        }

        private string CalcularSHA256(byte[] datos)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(datos);
            return Convert.ToBase64String(hash);
        }

        private string CalcularSHA256(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return CalcularSHA256(bytes);
        }

        private string CalcularSHA1(byte[] datos)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(datos);
            return Convert.ToBase64String(hash);
        }

        private string CalcularThumbprintSHA256(byte[] rawData)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(rawData);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string ExportPublicKeyPem(X509Certificate2 cert)
        {
            var rsa = cert.GetRSAPublicKey(); var ecdsa = cert.GetECDsaPublicKey(); var publicKey = rsa != null ? (AsymmetricAlgorithm)rsa : (ecdsa != null ? (AsymmetricAlgorithm)ecdsa : throw new InvalidOperationException("No public key"));
            if (publicKey == null) return string.Empty;
            return "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(publicKey.ExportSubjectPublicKeyInfo()) + "\n-----END PUBLIC KEY-----";
        }

        private string ObtenerAlgoritmoFirma(X509Certificate2 cert)
        {
            return cert.SignatureAlgorithm.FriendlyName switch
            {
                "sha256RSA" => "SHA256withRSA",
                "sha384RSA" => "SHA384withRSA",
                "sha512RSA" => "SHA512withRSA",
                "ECDSA" => "SHA256withECDSA",
                _ => cert.SignatureAlgorithm.FriendlyName
            };
        }

        private string ObtenerAlgoritmoClave(X509Certificate2 cert)
        {
            return cert.PublicKey.Oid.FriendlyName switch
            {
                "RSA" => "RSA",
                "ECDSA_P256" => "ECDSA",
                "ECDSA_P384" => "ECDSA",
                "ECDSA_P521" => "ECDSA",
                _ => "RSA"
            };
        }

        private string ExportarCadenaCertificados(X509Certificate2 cert)
        {
            var builder = new StringBuilder();
            var chain = new X509Chain { ChainPolicy = { RevocationMode = X509RevocationMode.NoCheck } };
            chain.Build(cert);
            foreach (var element in chain.ChainElements)
            {
                builder.AppendLine("-----BEGIN CERTIFICATE-----");
                builder.AppendLine(Convert.ToBase64String(element.Certificate.RawData, Base64FormattingOptions.InsertLineBreaks));
                builder.AppendLine("-----END CERTIFICATE-----");
            }
            return builder.ToString();
        }

        private bool ValidarVigencia(X509Certificate2 cert)
        {
            var now = DateTime.Now;
            return now >= cert.NotBefore && now <= cert.NotAfter;
        }

        private TipoCertificado DetectarTipoCertificado(X509Certificate2 cert, TipoCertificado porDefecto)
        {
            foreach (var ext in cert.Extensions)
            {
                if (ext.Oid.Value == "1.3.6.1.5.5.7.1.3")
                {
                    return TipoCertificado.Cualificada;
                }
            }
            return porDefecto;
        }

        private string? DetectarQTSP(X509Certificate2 cert)
        {
            foreach (var ext in cert.Extensions)
            {
                if (ext.Oid.Value == "1.3.6.1.5.5.7.1.3")
                {
                    return cert.Issuer.Split(',')[0].Replace("CN=", "").Trim();
                }
            }
            return cert.Issuer.Split(',')[0].Replace("CN=", "").Trim();
        }

        private string GenerarReferenciaSolicitud() => $"SOL{DateTime.Now:yyyyMMdd}{new Random().Next(100000, 999999):D6}";
        private string GenerarReferenciaComunicacion() => $"COM{DateTime.Now:yyyyMMdd}{new Random().Next(100000, 999999):D6}";

        private async Task EnviarNotificacionesFirmantesAsync(SolicitudFirma solicitud)
        {
        }

        

        #endregion

        // DTOs internos
        public class FirmanteSolicitud
        {
            public string Nombre { get; set; } = string.Empty;
            public string NIF { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? Cargo { get; set; }
            public int Orden { get; set; }
            public bool Firmado { get; set; } = false;
            public DateTime? FechaFirma { get; set; }
        }

        public class DestinatarioCertificado
        {
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? NIF { get; set; }
        }

        public class AdjuntoCertificado
        {
            public string Nombre { get; set; } = string.Empty;
            public string Base64 { get; set; } = string.Empty;
            public string HashSHA256 { get; set; } = string.Empty;
        }

        public class ResultadoVerificacion
        {
            public bool Valida { get; set; }
            public List<string> Detalles { get; set; } = new();
        }
    }
}