using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ERP.Domain.Entities.Bancario;

namespace ERP.Services.Bancario
{
    /// <summary>
    /// Servicio para generar ficheros SEPA ISO 20022 (pain.001 / pain.008)
    /// Cumple EPC Rulebooks y normativa Reglamento (UE) 260/2012
    /// </summary>
    public class SepaXmlGeneratorService
    {
        private const string NsPain001 = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03";
        private const string NsPain008 = "urn:iso:std:iso:20022:tech:xsd:pain.008.001.02";

        /// <summary>
        /// Genera XML pain.001.001.03 (SEPA Credit Transfer / SCT Inst)
        /// </summary>
        public string GenerarPain001(RemesaSEPA remesa)
        {
            if (remesa.Tipo != TipoRemesaSEPA.Transferencia)
                throw new ArgumentException("La remesa debe ser de tipo Transferencia para pain.001");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(NsPain001 + "Document",
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement(NsPain001 + "CstmrCdtTrfInitn",
                        GenerarGroupHeader(remesa),
                        GenerarPaymentInformationSCT(remesa)
                    )
                )
            );

            return FormatearXml(doc);
        }

        /// <summary>
        /// Genera XML pain.008.001.02 (SEPA Direct Debit Core / B2B)
        /// </summary>
        public string GenerarPain008(RemesaSEPA remesa)
        {
            if (remesa.Tipo != TipoRemesaSEPA.Adeudo)
                throw new ArgumentException("La remesa debe ser de tipo Adeudo para pain.008");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(NsPain008 + "Document",
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement(NsPain008 + "CstmrDrctDbtInitn",
                        GenerarGroupHeader(remesa),
                        GenerarPaymentInformationSDD(remesa)
                    )
                )
            );

            return FormatearXml(doc);
        }

        private XElement GenerarGroupHeader(RemesaSEPA remesa)
        {
            var msgId = $"MSG{remesa.Referencia}{DateTime.Now:yyyyMMddHHmmss}";
            var creDtTm = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            return new XElement("GrpHdr",
                new XElement("MsgId", msgId),
                new XElement("CreDtTm", creDtTm),
                new XElement("NbOfTxs", remesa.NumeroOperaciones.ToString()),
                new XElement("CtrlSum", remesa.ImporteTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("InitgPty",
                    new XElement("Nm", Truncate(remesa.Empresa?.NombreComercial ?? remesa.CuentaBancariaOrdenante?.NombreCuenta ?? "ERP", 70)),
                    new XElement("Id",
                        new XElement("OrgId",
                            new XElement("Othr",
                                new XElement("Id", remesa.CuentaBancariaOrdenante?.CreditorIdentifier ?? "NOTPROVIDED")
                            )
                        )
                    )
                )
            );
        }

        private XElement GenerarPaymentInformationSCT(RemesaSEPA remesa)
        {
            var pmtMtd = remesa.Esquema == EsquemaRemesaSEPA.SCT_Inst ? "INST" : "TRF";

            var dbtr = new XElement("Dbtr",
                new XElement("Nm", Truncate(remesa.Empresa?.NombreComercial ?? remesa.CuentaBancariaOrdenante?.NombreCuenta ?? "ERP", 70))
            );

            var dbtrAcct = new XElement("DbtrAcct",
                new XElement("Id",
                    new XElement("IBAN", remesa.CuentaBancariaOrdenante?.IBAN?.Replace(" ", ""))
                )
            );

            var dbtrAgt = new XElement("DbtrAgt",
                new XElement("FinInstnId",
                    new XElement("BICFI", remesa.CuentaBancariaOrdenante?.BIC ?? "NOTPROVIDED")
                )
            );

            var paymentInfos = new List<XElement>
            {
                new XElement("PmtInfId", Truncate(remesa.Referencia, 35)),
                new XElement("PmtMtd", remesa.Esquema == EsquemaRemesaSEPA.SCT_Inst ? "INST" : "TRF"),
                new XElement("BtchBookg", false),
                new XElement("NbOfTxs", remesa.NumeroOperaciones.ToString()),
                new XElement("CtrlSum", remesa.ImporteTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("PmtTpInf",
                    new XElement("SvcLvl", new XElement("Cd", "SEPA")),
                    new XElement("LclInstrm", new XElement("Cd", remesa.Esquema == EsquemaRemesaSEPA.SCT_Inst ? "INST" : "SEPA")),
                    new XElement("CtgyPurp", new XElement("Cd", "SUPP"))
                ),
                new XElement("ReqdExctnDt", remesa.FechaEjecucion.ToString("yyyy-MM-dd")),
                dbtr,
                dbtrAcct,
                dbtrAgt
            };

            int seq = 1;
            foreach (var op in remesa.Operaciones.OrderBy(o => o.Orden))
            {
                paymentInfos.Add(GenerarCdtTrfTxInf(op, seq++));
            }

            return new XElement("PmtInf", paymentInfos);
        }

        private XElement GenerarCdtTrfTxInf(OperacionRemesaSEPA op, int secuencia)
        {
            var endToEndId = !string.IsNullOrEmpty(op.ReferenciaPropia)
                ? Truncate(op.ReferenciaPropia, 35)
                : $"END{op.RemesaId}{op.Orden:D6}";

            var elementos = new List<XElement>
            {
                new XElement("PmtId",
                    new XElement("InstrId", $"INS{op.RemesaId}{op.Orden:D6}"),
                    new XElement("EndToEndId", endToEndId)
                ),
                new XElement("Amt",
                    new XElement("InstdAmt",
                        new XAttribute("Ccy", op.Moneda),
                        op.Importe.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    )
                ),
                new XElement("CdtrAgt",
                    new XElement("FinInstnId",
                        new XElement("BICFI", op.BeneficiarioBIC ?? "NOTPROVIDED")
                    )
                ),
                CrearCdtrElement(op),
                new XElement("CdtrAcct",
                    new XElement("Id",
                        new XElement("IBAN", op.BeneficiarioIBAN?.Replace(" ", ""))
                    )
                ),
                new XElement("RmtInf",
                    !string.IsNullOrEmpty(op.Concepto)
                        ? new XElement("Ustrd", Truncate(op.Concepto, 140))
                        : null
                )
            };

            return new XElement("CdtTrfTxInf", elementos.Where(e => e != null && (e.Elements().Any() || !string.IsNullOrEmpty(e.Value))));
        }

        private XElement CrearCdtrElement(OperacionRemesaSEPA op)
        {
            var nm = new XElement("Nm", Truncate(op.BeneficiarioNombre ?? "BENEFICIARIO", 70));
            var pstlAdr = CrearDireccionPostal(op.BeneficiarioCalle, op.BeneficiarioNumero, op.BeneficiarioCodigoPostal, op.BeneficiarioPoblacion, op.BeneficiarioPais);

            return new XElement("Cdtr",
                nm,
                pstlAdr,
                new XElement("CdtrAcct",
                    new XElement("Id",
                        new XElement("IBAN", op.BeneficiarioIBAN?.Replace(" ", ""))
                    )
                ),
                new XElement("RmtInf",
                    !string.IsNullOrEmpty(op.Concepto)
                        ? new XElement("Ustrd", Truncate(op.Concepto, 140))
                        : null
                )
            );
        }

        private XElement CrearDireccionPostal(string? calle, string? numero, string? cp, string? poblacion, string? pais)
        {
            var elementos = new List<XElement>();
            if (!string.IsNullOrEmpty(calle)) elementos.Add(new XElement("StrtNm", Truncate(calle, 70)));
            if (!string.IsNullOrEmpty(numero)) elementos.Add(new XElement("BldgNb", Truncate(numero, 16)));
            if (!string.IsNullOrEmpty(cp)) elementos.Add(new XElement("PstCd", Truncate(cp, 16)));
            if (!string.IsNullOrEmpty(poblacion)) elementos.Add(new XElement("TwnNm", Truncate(poblacion, 35)));
            if (!string.IsNullOrEmpty(pais)) elementos.Add(new XElement("Ctry", pais));

            if (!elementos.Any()) return null;

            return new XElement("PstlAdr", elementos);
        }

        private XElement GenerarPaymentInformationSDD(RemesaSEPA remesa)
        {
            var seqTp = remesa.Operaciones.FirstOrDefault()?.TipoSecuencia.ToString() ?? "RCUR";
            var lclInstrm = remesa.Esquema == EsquemaRemesaSEPA.SDD_B2B ? "B2B" : "CORE";

            var cdtr = new XElement("Cdtr",
                new XElement("Nm", Truncate(remesa.Empresa?.NombreComercial ?? remesa.CuentaBancariaAcreedora?.NombreCuenta ?? "ERP", 70)),
                new XElement("Id",
                    new XElement("OrgId",
                        new XElement("Othr",
                            new XElement("Id", remesa.CuentaBancariaAcreedora?.CreditorIdentifier ?? remesa.CuentaBancariaOrdenante?.CreditorIdentifier ?? "NOTPROVIDED")
                        )
                    )
                )
            );

            var cdtrAcct = new XElement("CdtrAcct",
                new XElement("Id",
                    new XElement("IBAN", remesa.CuentaBancariaAcreedora?.IBAN?.Replace(" ", "") ?? remesa.CuentaBancariaOrdenante?.IBAN?.Replace(" ", ""))
                )
            );

            var cdtrAgt = new XElement("CdtrAgt",
                new XElement("FinInstnId",
                    new XElement("BICFI", remesa.CuentaBancariaAcreedora?.BIC ?? remesa.CuentaBancariaOrdenante?.BIC ?? "NOTPROVIDED")
                )
            );

            var paymentInfos = new List<XElement>
            {
                new XElement("PmtInfId", Truncate(remesa.Referencia, 35)),
                new XElement("PmtMtd", "DD"),
                new XElement("BtchBookg", false),
                new XElement("NbOfTxs", remesa.NumeroOperaciones.ToString()),
                new XElement("CtrlSum", remesa.ImporteTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("PmtTpInf",
                    new XElement("SvcLvl", new XElement("Cd", "SEPA")),
                    new XElement("LclInstrm", new XElement("Cd", lclInstrm)),
                    new XElement("SeqTp", seqTp),
                    new XElement("CtgyPurp", new XElement("Cd", "SUPP"))
                ),
                new XElement("ReqdColltnDt", remesa.FechaEjecucion.ToString("yyyy-MM-dd")),
                cdtr,
                cdtrAcct,
                cdtrAgt
            };

            int seq = 1;
            foreach (var op in remesa.Operaciones.OrderBy(o => o.Orden))
            {
                paymentInfos.Add(GenerarDrctDbtTxInf(op, seq++));
            }

            return new XElement("PmtInf", paymentInfos);
        }

        private XElement GenerarDrctDbtTxInf(OperacionRemesaSEPA op, int secuencia)
        {
            var endToEndId = !string.IsNullOrEmpty(op.ReferenciaPropia)
                ? Truncate(op.ReferenciaPropia, 35)
                : $"END{op.RemesaId}{op.Orden:D6}";

            var mandateId = op.ReferenciaUnicaMandato ?? op.Mandato?.ReferenciaUnicaMandato ?? $"MND{op.RemesaId}{op.Orden:D6}";

            var drctDbtTx = new XElement("DrctDbtTx",
                new XElement("MndtRltdInf",
                    new XElement("MndtId", mandateId),
                    new XElement("DtOfSgntr", op.Mandato?.FechaFirma.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd")),
                    new XElement("AmdmntInd", false),
                    new XElement("ElctrncSgntr", true)
                )
            );

            var dbtr = new XElement("Dbtr",
                new XElement("Nm", Truncate(op.DeudorNombre ?? op.Mandato?.DeudorNombre ?? "DEUDOR", 70)),
                CrearDireccionPostal(op.Mandato?.DeudorCalle, op.Mandato?.DeudorNumero, op.Mandato?.DeudorCodigoPostal, op.Mandato?.DeudorPoblacion, op.Mandato?.DeudorPais),
                new XElement("Id",
                    new XElement("OrgId",
                        new XElement("Othr",
                            new XElement("Id", op.Mandato?.DeudorIdentificacionFiscal ?? "NOTPROVIDED")
                        )
                    )
                )
            );

            var dbtrAcct = new XElement("DbtrAcct",
                new XElement("Id",
                    new XElement("IBAN", op.DeudorIBAN?.Replace(" ", "") ?? op.Mandato?.DeudorIBAN?.Replace(" ", ""))
                )
            );

            var dbtrAgt = new XElement("DbtrAgt",
                new XElement("FinInstnId",
                    new XElement("BICFI", op.DeudorBIC ?? op.Mandato?.DeudorBIC ?? "NOTPROVIDED")
                )
            );

            var rmtInf = !string.IsNullOrEmpty(op.Concepto)
                ? new XElement("RmtInf", new XElement("Ustrd", Truncate(op.Concepto, 140)))
                : null;

            var elementos = new List<XElement>
            {
                new XElement("PmtId",
                    new XElement("InstrId", $"INS{op.RemesaId}{op.Orden:D6}"),
                    new XElement("EndToEndId", !string.IsNullOrEmpty(op.ReferenciaPropia) ? Truncate(op.ReferenciaPropia, 35) : $"END{op.RemesaId}{op.Orden:D6}")
                ),
                new XElement("PmtTpInf",
                    new XElement("SvcLvl", new XElement("Cd", "SEPA")),
                    new XElement("LclInstrm", new XElement("Cd", op.Remesa?.Esquema == EsquemaRemesaSEPA.SDD_B2B ? "B2B" : "CORE")),
                    new XElement("SeqTp", op.TipoSecuencia.ToString())
                ),
                new XElement("InstdAmt",
                    new XAttribute("Ccy", op.Moneda),
                    op.Importe.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                ),
                drctDbtTx,
                dbtr,
                dbtrAcct,
                dbtrAgt,
                rmtInf
            };

            return new XElement("DrctDbtTxInf", elementos.Where(e => e != null && (e.Elements().Any() || !string.IsNullOrEmpty(e.Value))));
        }

        private string FormatearXml(XDocument doc)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var ms = new MemoryStream();
            using var writer = XmlWriter.Create(ms, settings);
            doc.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}