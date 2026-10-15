using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketBai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacturasSustituidasJson",
                table: "Documentos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HaciendaForal",
                table: "Documentos",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RechazoPrevio",
                table: "Documentos",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefExterna",
                table: "Documentos",
                type: "TEXT",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionesTicketBai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    HaciendaForal = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    CertificadoSoftwareNombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CertificadoSoftwareHuella = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IdCertificadoSoftware = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NombreSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VersionSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IdSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroInstalacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EndpointEnvio = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EndpointConsulta = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EndpointAnulacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IntentosMaximosEnvio = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervaloReintentoMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    EnvioAutomatico = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioUltimaActualizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesTicketBai", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesTicketBai_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketBaiRegistros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NifEmisor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NombreRazonEmisor = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    NumeroFactura = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Serie = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FechaExpedicion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipoFactura = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    TipoRectificativa = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    Incidencia = table.Column<bool>(type: "INTEGER", nullable: false),
                    RechazoPrevio = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefExterna = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    FacturasRectificadasJson = table.Column<string>(type: "TEXT", nullable: true),
                    BaseImponible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CuotaTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CuotaRecargoEquivalencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CodMoneda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DescripcionOperacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaHoraHusoGeneracion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EsPrimerRegistro = table.Column<bool>(type: "INTEGER", nullable: false),
                    HuellaAnterior = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Huella = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TipoHuella = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    NombreSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VersionSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IdSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroInstalacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    HaciendaForal = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    CertificadoSoftware = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CertificadoSoftwareHuella = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IdCertificadoSoftware = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FirmaRegistroBase64 = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EstadoEnvio = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaEnvio = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IntentosEnvio = table.Column<int>(type: "INTEGER", nullable: false),
                    RespuestaHacienda = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoErrorHacienda = table.Column<string>(type: "TEXT", nullable: true),
                    UrlQrTicketBai = table.Column<string>(type: "TEXT", nullable: true),
                    DatosRegistroJson = table.Column<string>(type: "TEXT", nullable: false),
                    FechaAnulacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AnulacionPreviaId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketBaiRegistros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketBaiRegistros_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketBaiRegistros_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketBaiAnulaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegistroAltaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NifEmisor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NumeroFactura = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    FechaExpedicion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHoraHusoGeneracion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    HuellaAnterior = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Huella = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EstadoEnvio = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaEnvio = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IntentosEnvio = table.Column<int>(type: "INTEGER", nullable: false),
                    RespuestaHacienda = table.Column<string>(type: "TEXT", nullable: true),
                    DatosRegistroJson = table.Column<string>(type: "TEXT", nullable: false),
                    FechaAnulacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketBaiAnulaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketBaiAnulaciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketBaiAnulaciones_TicketBaiRegistros_RegistroAltaId",
                        column: x => x.RegistroAltaId,
                        principalTable: "TicketBaiRegistros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesTicketBai_EmpresaId_HaciendaForal",
                table: "ConfiguracionesTicketBai",
                columns: new[] { "EmpresaId", "HaciendaForal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketBaiAnulaciones_EmpresaId",
                table: "TicketBaiAnulaciones",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBaiAnulaciones_RegistroAltaId",
                table: "TicketBaiAnulaciones",
                column: "RegistroAltaId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBaiRegistros_DocumentoId",
                table: "TicketBaiRegistros",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBaiRegistros_EmpresaId_Codigo",
                table: "TicketBaiRegistros",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketBaiRegistros_EmpresaId_EstadoEnvio_FechaCreacion",
                table: "TicketBaiRegistros",
                columns: new[] { "EmpresaId", "EstadoEnvio", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesTicketBai");

            migrationBuilder.DropTable(
                name: "TicketBaiAnulaciones");

            migrationBuilder.DropTable(
                name: "TicketBaiRegistros");

            migrationBuilder.DropColumn(
                name: "FacturasSustituidasJson",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "HaciendaForal",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RechazoPrevio",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "RefExterna",
                table: "Documentos");
        }
    }
}
