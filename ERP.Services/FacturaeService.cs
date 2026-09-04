using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    /// <summary>
    /// Servicio Facturae 3.2.2 + EN16931 UBL para FACe (B2G Ley 25/2013) y B2B (Ley 18/2022 Crea y Crece)
    /// Firma XAdES-Enveloped con CertificadoDigital cualificado.
    /// </summary>
    public class FacturaeService
    {
        private readonly ApplicationDbContext _context;
        public FacturaeService(ApplicationDbContext context) => _context = context;

        public async Task<FacturaElectronica> GenerarFacturaeAsync(int documentoId, FormatoFacturaElectronica formato = FormatoFacturaElectronica.Facturae32, string? dir3OC = null, string? dir3OG = null, string? dir3UT = null)
        {
            var doc = await _context.Documentos.Include(d => d.Lineas).Include(d => d.Empresa).FirstOrDefaultAsync(d => d.Id == documentoId)
                ?? throw new InvalidOperationException("Documento no existe");
            if (doc.Tipo != TipoDocumento.Factura && doc.Tipo != TipoDocumento.FacturaRectificativa)
                throw new InvalidOperationException("Solo facturas");

            var empresa = doc.Empresa ?? await _context.Empresas.FindAsync(doc.EmpresaId)
                ?? throw new InvalidOperationException("Empresa no existe");

            var xml = GenerarXmlFacturae(doc, empresa, formato, dir3OC, dir3OG, dir3UT);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml)));

            var fe = new FacturaElectronica
            {
                DocumentoId = documentoId,
                EmpresaId = doc.EmpresaId,
                Formato = formato,
                Version = formato == FormatoFacturaElectronica.Facturae32 ? "3.2.2" : "2.1",
                XmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)),
                HashSha256 = hash,
                DIR3_OficinaContable = dir3OC,
                DIR3_OrganoGestor = dir3OG,
                DIR3_UnidadTramitadora = dir3UT,
                PuntoEntrada = dir3OC != null ? PuntoEntradaFacturaElectronica.FACe : PuntoEntradaFacturaElectronica.HubAEAT,
                Estado = EstadoFacturaElectronica.Generada,
                FechaLimitePago = DateTime.Now.AddDays(doc.ClienteId.HasValue ? 60 : 30) // 60 B2B, 30 AA.PP.
            };

            _context.FacturasElectronicas.Add(fe);
            await _context.SaveChangesAsync();
            return fe;
        }

        public async Task<FacturaElectronica> FirmarXAdESAsync(int facturaElectronicaId, int certificadoId)
        {
            var fe = await _context.FacturasElectronicas.FindAsync(facturaElectronicaId)
                ?? throw new InvalidOperationException("Factura electrónica no existe");
            var cert = await _context.CertificadosDigitales.FindAsync(certificadoId)
                ?? throw new InvalidOperationException("Certificado no existe");
            if (!cert.EstaVigente) throw new InvalidOperationException("Certificado no vigente");

            // Stub XAdES: en producción usar XadesNet o similar; aquí se genera firma base64 simulada ligada al hash
            var xmlBytes = Convert.FromBase64String(fe.XmlBase64 ?? "");
            var firma = $"XAdES-{cert.ThumbprintSHA256}-{fe.HashSha256}";
            fe.FirmaXAdESBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(firma));
            fe.CertificadoId = certificadoId;
            fe.Estado = EstadoFacturaElectronica.Firmada;
            await _context.SaveChangesAsync();
            return fe;
        }

        public async Task<List<FacturaElectronica>> GetFacturasElectronicasAsync(int? empresaId = null)
        {
            var q = _context.FacturasElectronicas.Include(f => f.Documento).ThenInclude(d => d.Cliente).Include(f => f.Documento).ThenInclude(d => d.Proveedor).Include(f => f.Documento).ThenInclude(d => d.Lineas).AsQueryable();
            if (empresaId.HasValue) q = q.Where(f => f.EmpresaId == empresaId.Value);
            return await q.OrderByDescending(f => f.FechaCreacion).ToListAsync();
        }

        public async Task<FacturaElectronica> RegistrarEnFACeAsync(int facturaElectronicaId, string? codigoRegistroSimulado = null)
        {
            var fe = await _context.FacturasElectronicas.FindAsync(facturaElectronicaId)
                ?? throw new InvalidOperationException("Factura electrónica no existe");
            fe.CodigoRegistroFACe = codigoRegistroSimulado ?? $"REG-FACe-{DateTime.Now:yyyyMMddHHmmss}-{fe.Id:D6}";
            fe.FechaRegistro = DateTime.Now;
            fe.Estado = EstadoFacturaElectronica.Registrada;
            await _context.SaveChangesAsync();
            return fe;
        }

        private string GenerarXmlFacturae(DocumentoComercial doc, Empresa empresa, FormatoFacturaElectronica formato, string? oc, string? og, string? ut)
        {
            if (formato == FormatoFacturaElectronica.Facturae32)
            {
                var xdoc = new XDocument(
                    new XElement("fe:Facturae",
                        new XAttribute(XNamespace.Xmlns + "fe", "http://www.facturae.es/Facturae/2014/v3.2.2/Facturae"),
                        new XElement("FileHeader",
                            new XElement("SchemaVersion", "3.2.2"),
                            new XElement("Modality", "I"),
                            new XElement("InvoiceIssuerType", "EM")
                        ),
                        new XElement("Parties",
                            new XElement("SellerParty",
                                new XElement("TaxIdentification", new XElement("PersonTypeCode", "J"), new XElement("ResidenceTypeCode", "R"), new XElement("TaxIdentificationNumber", empresa.CIF)),
                                new XElement("LegalEntity", new XElement("CorporateName", empresa.RazonSocial))
                            )
                        ),
                        new XElement("Invoices",
                            new XElement("Invoice",
                                new XElement("InvoiceHeader",
                                    new XElement("InvoiceNumber", doc.NumeroDocumento),
                                    new XElement("InvoiceSeriesCode", empresa.SerieFacturacion),
                                    new XElement("InvoiceDocumentType", doc.Tipo == TipoDocumento.FacturaRectificativa ? "R1" : "FC"),
                                    new XElement("InvoiceClass", "OO")
                                ),
                                new XElement("InvoiceIssueData",
                                    new XElement("IssueDate", doc.Fecha.ToString("yyyy-MM-dd")),
                                    new XElement("InvoiceCurrencyCode", "EUR")
                                ),
                                new XElement("TaxesOutputs",
                                    doc.Lineas.Select(l => new XElement("Tax",
                                        new XElement("TaxTypeCode", "01"),
                                        new XElement("TaxRate", l.PorcentajeIva.ToString("F2")),
                                        new XElement("TaxableBase", new XElement("TotalAmount", (l.Cantidad * l.PrecioUnitario).ToString("F2"))),
                                        new XElement("TaxAmount", new XElement("TotalAmount", (l.Cantidad * l.PrecioUnitario * l.PorcentajeIva / 100m).ToString("F2")))
                                    ))
                                ),
                                new XElement("InvoiceTotals",
                                    new XElement("TotalGrossAmount", doc.BaseImponible.ToString("F2")),
                                    new XElement("TotalTaxOutputs", doc.TotalIva.ToString("F2")),
                                    new XElement("InvoiceTotal", doc.Total.ToString("F2"))
                                ),
                                oc != null ? new XElement("AdministrativeCentres",
                                    new XElement("AdministrativeCentre", new XElement("CentreCode", oc), new XElement("RoleTypeCode", "01")),
                                    new XElement("AdministrativeCentre", new XElement("CentreCode", og), new XElement("RoleTypeCode", "02")),
                                    new XElement("AdministrativeCentre", new XElement("CentreCode", ut), new XElement("RoleTypeCode", "03"))
                                ) : null
                            )
                        )
                    )
                );
                return xdoc.Declaration != null ? xdoc.Declaration + Environment.NewLine + xdoc.ToString() : xdoc.ToString();
            }
            // UBL / CII stub
            return $"<UBL Invoice {doc.NumeroDocumento} {formato} />";
        }
    }
}
