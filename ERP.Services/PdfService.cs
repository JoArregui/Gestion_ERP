using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using QRCoder;

namespace ERP.Services
{
    public class PdfService
    {
        public PdfService()
        {
            // Configuración de licencia para uso comunitario
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region Generación de Factura y Ticket

        public byte[] GenerarFacturaPdf(DocumentoComercial factura, Empresa empresa, Cliente? cliente)
        {
            var colorMarca = string.IsNullOrWhiteSpace(empresa.ColorHex) ? "#1e293b" : empresa.ColorHex;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(empresa.NombreComercial?.ToUpper() ?? "ERP SYSTEM").FontSize(24).ExtraBold().FontColor(colorMarca);
                            col.Item().Text(empresa.RazonSocial).FontSize(10).SemiBold();
                            col.Item().PaddingTop(2).Column(c => {
                                c.Item().Text($"CIF: {empresa.CIF}").FontSize(9);
                                c.Item().Text(empresa.Direccion ?? "Dirección no configurada").FontSize(9);
                                if (!string.IsNullOrEmpty(empresa.Telefono)) 
                                    c.Item().Text($"Teléfono: {empresa.Telefono}").FontSize(9);
                            });
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Background(colorMarca).PaddingHorizontal(10).PaddingVertical(5).Text(factura.Tipo.ToString().ToUpper())
                                .FontSize(20).ExtraBold().FontColor(Colors.White);
                            
                            col.Item().PaddingTop(10).Text($"Nº DOCUMENTO: {factura.NumeroDocumento}").Bold();
                            col.Item().Text($"FECHA EMISIÓN: {factura.Fecha:dd/MM/yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(0.5f); 
                            row.RelativeItem(0.5f).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                            {
                                c.Item().Text("DATOS FISCALES DEL RECEPTOR").FontSize(8).ExtraBold().FontColor(colorMarca);
                                c.Item().PaddingTop(4).Text(cliente?.RazonSocial ?? "CLIENTE CONTADO").Bold().FontSize(11);
                                c.Item().Text($"CIF/NIF: {cliente?.CIF ?? "N/A"}");
                                c.Item().Text(cliente?.Direccion ?? "");
                            });
                        });

                        col.Item().PaddingTop(30);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("DESCRIPCIÓN");
                                header.Cell().Element(CellStyle).AlignRight().Text("CANT.");
                                header.Cell().Element(CellStyle).AlignRight().Text("PRECIO");
                                header.Cell().Element(CellStyle).AlignRight().Text("SUBTOTAL");

                                IContainer CellStyle(IContainer c) => c.PaddingVertical(8).BorderBottom(2).BorderColor(colorMarca);
                            });

                            foreach (var linea in factura.Lineas)
                            {
                                table.Cell().Element(ContentStyle).Text(linea.DescripcionArticulo);
                                table.Cell().Element(ContentStyle).AlignRight().Text(linea.Cantidad.ToString("N2"));
                                table.Cell().Element(ContentStyle).AlignRight().Text($"{linea.PrecioUnitario:N2} €");
                                table.Cell().Element(ContentStyle).AlignRight().Text($"{(linea.Cantidad * linea.PrecioUnitario):N2} €");

                                IContainer ContentStyle(IContainer c) => c.PaddingVertical(8).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
                            }
                        });

                        col.Item().AlignRight().PaddingTop(20).MinWidth(200).Column(c =>
                        {
                            c.Item().Row(r => {
                                r.RelativeItem().Text("Base Imponible:").AlignRight();
                                r.RelativeItem().Text($"{factura.BaseImponible:N2} €").AlignRight();
                            });
                            c.Item().Row(r => {
                                r.RelativeItem().Text("Cuota I.V.A.:").AlignRight();
                                r.RelativeItem().Text($"{factura.TotalIva:N2} €").AlignRight();
                            });
                            c.Item().PaddingTop(5).Background(colorMarca).Padding(8).Row(r => {
                                r.RelativeItem().Text("TOTAL").FontColor(Colors.White).ExtraBold().FontSize(12);
                                r.RelativeItem().Text($"{factura.Total:N2} €").FontColor(Colors.White).ExtraBold().FontSize(12).AlignRight();
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarTicketPdf(DocumentoComercial factura, Empresa empresa, Cliente? cliente)
        {
            string qrContent = $"https://tu-sistema-erp.com/f/{factura.NumeroDocumento}";
            byte[] qrImage = GenerarQrByte(qrContent);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(227, 600, Unit.Point); 
                    page.Margin(10, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Courier"));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(empresa.NombreComercial?.ToUpper() ?? "TIENDA").FontSize(14).ExtraBold();
                        col.Item().AlignCenter().Text($"CIF: {empresa.CIF}").FontSize(8);
                        col.Item().AlignCenter().Text("------------------------------------------");
                        col.Item().Text($"FECHA: {factura.Fecha:dd/MM/yyyy HH:mm}");
                        col.Item().Text($"DOC: {factura.NumeroDocumento}");
                        col.Item().AlignCenter().Text("------------------------------------------");
                    });

                    page.Content().Column(col =>
                    {
                        foreach (var linea in factura.Lineas)
                        {
                            col.Item().Row(row => {
                                row.RelativeItem(3).Text(linea.DescripcionArticulo);
                                row.RelativeItem().AlignRight().Text(linea.Cantidad.ToString("0"));
                                row.RelativeItem(1.5f).AlignRight().Text($"{(linea.Cantidad * linea.PrecioUnitario):N2}");
                            });
                        }
                        col.Item().PaddingTop(5).AlignCenter().Text("------------------------------------------");
                        col.Item().AlignRight().Text($"TOTAL: {factura.Total:N2} €").Bold().FontSize(12);
                        
                        col.Item().PaddingTop(10).AlignCenter().Width(80).Image(qrImage);
                        col.Item().AlignCenter().Text("Gracias por su compra").FontSize(7).Italic();
                    });
                });
            }).GeneratePdf();
        }

        #endregion

        #region Generación de Cierre de Caja

        public byte[] GenerarCierreCajaPdf(CierreCaja cierre, Empresa empresa)
        {
            var colorMarca = string.IsNullOrWhiteSpace(empresa.ColorHex) ? "#1e293b" : empresa.ColorHex;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("CIERRE DE CAJA (Z)").FontSize(22).ExtraBold().FontColor(colorMarca);
                            col.Item().Text($"{empresa.RazonSocial}").SemiBold().FontSize(12);
                            col.Item().Text($"Terminal: {cierre.Terminal} | ID: #{cierre.Id}").FontSize(9);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(cierre.FechaCierre.ToString("f")).FontSize(9).FontColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(5).Text("DOCUMENTO CONTABLE").FontSize(8).Bold().FontColor(colorMarca);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // 1. RESUMEN DE VENTAS Y ARQUEO
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns => {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(HeaderStyle).Text("CONCEPTO").SemiBold();
                            table.Cell().Element(HeaderStyle).AlignRight().Text("IMPORTE").SemiBold();

                            table.Cell().Element(RowStyle).Text("Ventas Efectivo (Sistema)");
                            table.Cell().Element(RowStyle).AlignRight().Text($"{cierre.TotalVentasEfectivo:N2} €");

                            table.Cell().Element(RowStyle).Text("Ventas Tarjeta");
                            table.Cell().Element(RowStyle).AlignRight().Text($"{cierre.TotalVentasTarjeta:N2} €");

                            table.Cell().PaddingVertical(10).Text("EFECTIVO REAL EN CAJÓN").Bold();
                            table.Cell().PaddingVertical(10).AlignRight().Text($"{cierre.ImporteRealEnCaja:N2} €").Bold();

                            var colorDescuadre = Math.Abs(cierre.Descuadre) < 0.01m ? Colors.Green.Medium : Colors.Red.Medium;
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(10).Text("DIFERENCIA (DESCUADRE)").ExtraBold();
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(10).AlignRight().Text($"{cierre.Descuadre:N2} €").ExtraBold().FontColor(colorDescuadre);

                            IContainer HeaderStyle(IContainer c) => c.PaddingVertical(5).BorderBottom(1);
                            IContainer RowStyle(IContainer c) => c.PaddingVertical(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                        });

                        // 2. AUDITORÍA DE OPERADORES
                        if (!string.IsNullOrEmpty(cierre.DataUsuariosJson))
                        {
                            var usuarios = JsonSerializer.Deserialize<List<ResumenItem>>(cierre.DataUsuariosJson);
                            if (usuarios != null && usuarios.Any())
                            {
                                col.Item().PaddingTop(25).Text("AUDITORÍA DE OPERADORES").FontSize(11).ExtraBold().FontColor(colorMarca);
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns => {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });
                                    table.Header(h => {
                                        h.Cell().Element(HStyle).Text("Usuario");
                                        h.Cell().Element(HStyle).AlignRight().Text("Total");
                                        IContainer HStyle(IContainer c) => c.BorderBottom(1).PaddingVertical(5).DefaultTextStyle(s => s.Bold().FontSize(9));
                                    });
                                    foreach (var u in usuarios) {
                                        table.Cell().Element(RStyle).Text(u.Nombre);
                                        table.Cell().Element(RStyle).AlignRight().Text($"{u.Total:N2} €");
                                    }
                                    IContainer RStyle(IContainer c) => c.PaddingVertical(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten4).DefaultTextStyle(s => s.FontSize(9));
                                });
                            }
                        }

                        // 3. DESGLOSE DE IVA
                        col.Item().PaddingTop(25).Text("DESGLOSE IMPOSITIVO").FontSize(11).ExtraBold().FontColor(colorMarca);
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns => {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(h => {
                                h.Cell().Element(HeadStyle).Text("Tipo IVA");
                                h.Cell().Element(HeadStyle).AlignRight().Text("Base Imponible");
                                h.Cell().Element(HeadStyle).AlignRight().Text("Cuota");
                                IContainer HeadStyle(IContainer c) => c.BorderBottom(1).PaddingVertical(5).DefaultTextStyle(s => s.Bold().FontSize(9));
                            });

                            if (cierre.Base21 > 0) AddIvaRow(table, "IVA General (21%)", cierre.Base21, cierre.Iva21);
                            if (cierre.Base10 > 0) AddIvaRow(table, "IVA Reducido (10%)", cierre.Base10, cierre.Iva10);
                            if (cierre.Base4 > 0) AddIvaRow(table, "IVA Superred. (4%)", cierre.Base4, cierre.Iva4);

                            table.Cell().ColumnSpan(2).PaddingVertical(8).AlignRight().Text("TOTAL IVA:").Bold();
                            table.Cell().PaddingVertical(8).AlignRight().Text($"{cierre.TotalIva:N2} €").Bold();

                            void AddIvaRow(TableDescriptor t, string etiqueta, decimal baseI, decimal cuota) {
                                t.Cell().Element(RIvaStyle).Text(etiqueta);
                                t.Cell().Element(RIvaStyle).AlignRight().Text($"{baseI:N2} €");
                                t.Cell().Element(RIvaStyle).AlignRight().Text($"{cuota:N2} €");
                            }
                            IContainer RIvaStyle(IContainer c) => c.PaddingVertical(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).DefaultTextStyle(s => s.FontSize(9));
                        });

                        // 4. RANGO HORARIO Y CONTROL
                        col.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Lighten3).PaddingTop(10).Row(row => {
                            row.RelativeItem().Text(t => {
                                t.Span("Apertura Turno: ").FontSize(8).Bold();
                                t.Span(cierre.FechaCierre.AddHours(-8).ToString("HH:mm") + " hrs").FontSize(8); 
                            });
                            row.RelativeItem().AlignRight().Text(t => {
                                t.Span("Cierre Turno: ").FontSize(8).Bold();
                                t.Span(cierre.FechaCierre.ToString("HH:mm") + " hrs").FontSize(8);
                            });
                        });

                        // 5. OBSERVACIONES
                        if (!string.IsNullOrWhiteSpace(cierre.Observaciones))
                        {
                            col.Item().PaddingTop(20).Background(Colors.Grey.Lighten5).Padding(10).Column(c => {
                                c.Item().Text("NOTAS DE CIERRE:").FontSize(8).Bold();
                                c.Item().Text(cierre.Observaciones).FontSize(9).Italic();
                            });
                        }
                    });

                    page.Footer().PaddingTop(40).AlignCenter().Text(x => {
                        x.Span("Firma Responsable: ___________________________________ ").FontSize(10);
                    });
                });
            }).GeneratePdf();
        }

        #endregion

        #region Generación de Nómina

        public byte[] GenerarNominaPdf(Nomina nomina, Empleado empleado, Empresa empresa)
        {
            var colorMarca = string.IsNullOrWhiteSpace(empresa.ColorHex) ? "#1e293b" : empresa.ColorHex;
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(empresa.NombreComercial?.ToUpper() ?? "ERP SYSTEM").FontSize(22).ExtraBold().FontColor(colorMarca);
                            col.Item().Text(empresa.RazonSocial).FontSize(10).SemiBold();
                            col.Item().Text($"CIF: {empresa.CIF}  |  {empresa.Direccion}").FontSize(8);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Background(colorMarca).Padding(10).Text("NÓMINA").FontSize(20).ExtraBold().FontColor(Colors.White).AlignCenter();
                            col.Item().PaddingTop(8).Text($"Mes: {nomina.Mes:D2}/{nomina.Anio}").Bold().FontSize(11).AlignRight();
                            col.Item().Text($"Emisión: {nomina.FechaEmision:dd/MM/yyyy}").FontSize(8).AlignRight();
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                        {
                            c.Item().Text("DATOS DEL TRABAJADOR").FontSize(8).ExtraBold().FontColor(colorMarca);
                            c.Item().PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Column(cc => { cc.Item().Text($"{empleado.Nombre} {empleado.Apellidos}").Bold().FontSize(12); cc.Item().Text($"DNI: {empleado.DNI}  |  NSS: {empleado.NumeroSeguridadSocial}").FontSize(9); cc.Item().Text($"Cargo: {empleado.Cargo}  |  Depto: {empleado.Departamento}").FontSize(9); });
                                r.RelativeItem().AlignRight().Column(cc => { cc.Item().Text($"ID: #{empleado.Id}").FontSize(9).AlignRight(); cc.Item().Text($"IBAN: {empleado.IBAN ?? "—"}").FontSize(8).AlignRight(); });
                            });
                        });

                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.RelativeColumn(1.5f); });
                            table.Header(h => { h.Cell().Element(HeaderStyle).Text("CONCEPTO"); h.Cell().Element(HeaderStyle).AlignRight().Text("IMPORTE"); IContainer HeaderStyle(IContainer c) => c.PaddingVertical(8).BorderBottom(2).BorderColor(colorMarca).DefaultTextStyle(s => s.Bold()); });
                            table.Cell().Element(RowStyle).Text("Salario Base"); table.Cell().Element(RowStyle).AlignRight().Text($"{nomina.SalarioBase:N2} €");
                            table.Cell().Element(RowStyle).Text("Complementos (H. Extra)"); table.Cell().Element(RowStyle).AlignRight().Text($"+{nomina.Complementos:N2} €").FontColor(Colors.Green.Darken1);
                            table.Cell().Element(RowStyle).Text("Deducciones (15% SS/IRPF)"); table.Cell().Element(RowStyle).AlignRight().Text($"-{nomina.Deducciones:N2} €").FontColor(Colors.Red.Medium);
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(10).Text("TOTAL NETO").ExtraBold(); table.Cell().Background(colorMarca).Padding(10).AlignRight().Text($"{nomina.TotalNeto:N2} €").FontColor(Colors.White).ExtraBold().FontSize(12);
                            IContainer RowStyle(IContainer c) => c.PaddingVertical(8).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
                        });

                        col.Item().PaddingTop(20).Row(r =>
                        {
                            r.RelativeItem().Column(c => { c.Item().Text("ESTADO").FontSize(8).Bold().FontColor(colorMarca); c.Item().Text(nomina.EstaPagada ? "PAGADA" : "PENDIENTE").FontSize(11).ExtraBold().FontColor(nomina.EstaPagada ? Colors.Green.Medium : Colors.Amber.Medium); });
                            r.RelativeItem().AlignRight().Column(c => { c.Item().Text("Vencimiento").FontSize(8).Bold().FontColor(colorMarca).AlignRight(); c.Item().Text(new DateTime(nomina.Anio, nomina.Mes, DateTime.DaysInMonth(nomina.Anio, nomina.Mes)).ToString("dd/MM/yyyy")).AlignRight(); });
                        });
                    });

                    page.Footer().AlignCenter().Text($"Documento generado por {empresa.NombreComercial} el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        #endregion

        private byte[] GenerarQrByte(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        private class ResumenItem
        {
            private string _nombre = "General";

            [System.Text.Json.Serialization.JsonPropertyName("Categoria")]
            public string? CategoriaNombre { set { if (!string.IsNullOrEmpty(value)) _nombre = value; } }

            [System.Text.Json.Serialization.JsonPropertyName("Usuario")]
            public string? UsuarioNombre { set { if (!string.IsNullOrEmpty(value)) _nombre = value; } }

            public string Nombre { get => _nombre; set => _nombre = value; }
            public decimal Total { get; set; }
        }
    }
}