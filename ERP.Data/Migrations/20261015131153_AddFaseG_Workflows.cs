using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseG_Workflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificacionPlantillas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Asunto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CuerpoHtml = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Canal = table.Column<int>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionPlantillas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacionPlantillas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefiniciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EntidadObjetivo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AccionDisparadora = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CondicionJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AccionesJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Prioridad = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefiniciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowDefiniciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstancias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkflowDefinicionId = table.Column<int>(type: "INTEGER", nullable: false),
                    EntidadObjetivo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntidadObjetivoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioAsignado = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstancias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowInstancias_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowInstancias_WorkflowDefiniciones_WorkflowDefinicionId",
                        column: x => x.WorkflowDefinicionId,
                        principalTable: "WorkflowDefiniciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowPasos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoAccion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ParametrosJson = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEjecucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EjecutadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResultadoJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ErrorJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowPasos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowPasos_WorkflowInstancias_InstanciaId",
                        column: x => x.InstanciaId,
                        principalTable: "WorkflowInstancias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionPlantillas_EmpresaId_Codigo",
                table: "NotificacionPlantillas",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefiniciones_EmpresaId_Codigo",
                table: "WorkflowDefiniciones",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstancias_EmpresaId",
                table: "WorkflowInstancias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstancias_EntidadObjetivo_EntidadObjetivoId_Estado",
                table: "WorkflowInstancias",
                columns: new[] { "EntidadObjetivo", "EntidadObjetivoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstancias_WorkflowDefinicionId",
                table: "WorkflowInstancias",
                column: "WorkflowDefinicionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowPasos_InstanciaId",
                table: "WorkflowPasos",
                column: "InstanciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacionPlantillas");

            migrationBuilder.DropTable(
                name: "WorkflowPasos");

            migrationBuilder.DropTable(
                name: "WorkflowInstancias");

            migrationBuilder.DropTable(
                name: "WorkflowDefiniciones");
        }
    }
}
