using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCanalEtico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesCanalEtico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CanalWebActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanalEmailActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailCanalEtico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CanalTelefonoActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    TelefonoCanalEtico = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CanalPresencialActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanalAppMovilActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiasMaximosEvaluacion = table.Column<int>(type: "INTEGER", nullable: false),
                    DiasMaximosInvestigacion = table.Column<int>(type: "INTEGER", nullable: false),
                    PermiteAnonimas = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiereConfirmacionRecepcion = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificarInformanteCambios = table.Column<bool>(type: "INTEGER", nullable: false),
                    MedidasProteccionAutomaticas = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProhibicionRepresaliasExplicita = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResponsableCanalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResponsableProteccionDatosId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ComiteEticoIds = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TextoAcuseRecibo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TextoConfirmacionAnonimato = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioUltimaActualizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesCanalEtico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesCanalEtico_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DenunciasCanalEtico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    EsAnonima = table.Column<bool>(type: "INTEGER", nullable: false),
                    NombreInformante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EmailInformante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TelefonoInformante = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RelacionEmpresa = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DepartamentoArea = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DescripcionHechos = table.Column<string>(type: "TEXT", nullable: false),
                    TipoInfraccion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Gravedad = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FechaHechos = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LugarHechos = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PersonasImplicadas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PruebasAportadas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaAdmision = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaInvestigacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InstructorId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResponsableResolucionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Resolucion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TipoResolucion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MedidasCorrectoras = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SolicitaProteccion = table.Column<bool>(type: "INTEGER", nullable: false),
                    MedidasProteccion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    HaSidoRepresaliado = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetalleRepresalias = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    NotificarInformante = table.Column<bool>(type: "INTEGER", nullable: false),
                    ComunicacionesInternasJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioUltimaActualizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenunciasCanalEtico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DenunciasCanalEtico_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComentariosDenuncia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DenunciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EsInterno = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutorId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AutorNombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComentariosDenuncia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComentariosDenuncia_DenunciasCanalEtico_DenunciaId",
                        column: x => x.DenunciaId,
                        principalTable: "DenunciasCanalEtico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosDenuncia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DenunciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreOriginal = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TipoMime = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TamanoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FechaSubida = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioSubida = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosDenuncia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosDenuncia_DenunciasCanalEtico_DenunciaId",
                        column: x => x.DenunciaId,
                        principalTable: "DenunciasCanalEtico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialEstadosDenuncia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DenunciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "INTEGER", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstadosDenuncia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstadosDenuncia_DenunciasCanalEtico_DenunciaId",
                        column: x => x.DenunciaId,
                        principalTable: "DenunciasCanalEtico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosDenuncia_DenunciaId",
                table: "ComentariosDenuncia",
                column: "DenunciaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesCanalEtico_EmpresaId",
                table: "ConfiguracionesCanalEtico",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasCanalEtico_EmpresaId_Codigo",
                table: "DenunciasCanalEtico",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasCanalEtico_EmpresaId_Estado_FechaRecepcion",
                table: "DenunciasCanalEtico",
                columns: new[] { "EmpresaId", "Estado", "FechaRecepcion" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosDenuncia_DenunciaId",
                table: "DocumentosDenuncia",
                column: "DenunciaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosDenuncia_DenunciaId",
                table: "HistorialEstadosDenuncia",
                column: "DenunciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComentariosDenuncia");

            migrationBuilder.DropTable(
                name: "ConfiguracionesCanalEtico");

            migrationBuilder.DropTable(
                name: "DocumentosDenuncia");

            migrationBuilder.DropTable(
                name: "HistorialEstadosDenuncia");

            migrationBuilder.DropTable(
                name: "DenunciasCanalEtico");
        }
    }
}
