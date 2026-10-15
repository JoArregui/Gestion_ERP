using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseG3_ReportesI18nDivisa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportDefiniciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ConsultaSql = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ParametrosJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ColumnasJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    OpcionesJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Publico = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefiniciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDefiniciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportEjecuciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportDefinicionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParametrosJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FechaEjecucion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResultadoRuta = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TamanoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportEjecuciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportEjecuciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportEjecuciones_ReportDefiniciones_ReportDefinicionId",
                        column: x => x.ReportDefinicionId,
                        principalTable: "ReportDefiniciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefiniciones_EmpresaId_Codigo",
                table: "ReportDefiniciones",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportEjecuciones_EmpresaId_FechaEjecucion",
                table: "ReportEjecuciones",
                columns: new[] { "EmpresaId", "FechaEjecucion" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportEjecuciones_ReportDefinicionId",
                table: "ReportEjecuciones",
                column: "ReportDefinicionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportEjecuciones");

            migrationBuilder.DropTable(
                name: "ReportDefiniciones");
        }
    }
}
