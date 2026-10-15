using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseA_ActivosFijos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SetupTutorialCompletado",
                table: "UserEmpresas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SetupTutorialVisto",
                table: "UserEmpresas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ActivosFijos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CuentaActivoCodigo = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    CuentaAmortizacionCodigo = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    CuentaDotacionCodigo = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    PrecioAdquisicion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorResidual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaAdquisicion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaPuestaMarcha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VidaUtilAnios = table.Column<int>(type: "INTEGER", nullable: false),
                    Metodo = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    OrigenTipo = table.Column<string>(type: "TEXT", nullable: true),
                    OrigenId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MotivoBaja = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivosFijos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivosFijos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineasAmortizacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivoFijoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ejercicio = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaDotacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Dotacion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AmortizadoAcumulado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorNetoContable = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Contabilizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    AsientoContableId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasAmortizacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasAmortizacion_ActivosFijos_ActivoFijoId",
                        column: x => x.ActivoFijoId,
                        principalTable: "ActivosFijos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineasAmortizacion_AsientosContables_AsientoContableId",
                        column: x => x.AsientoContableId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivosFijos_EmpresaId",
                table: "ActivosFijos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasAmortizacion_ActivoFijoId",
                table: "LineasAmortizacion",
                column: "ActivoFijoId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasAmortizacion_AsientoContableId",
                table: "LineasAmortizacion",
                column: "AsientoContableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasAmortizacion");

            migrationBuilder.DropTable(
                name: "ActivosFijos");

            migrationBuilder.DropColumn(
                name: "SetupTutorialCompletado",
                table: "UserEmpresas");

            migrationBuilder.DropColumn(
                name: "SetupTutorialVisto",
                table: "UserEmpresas");
        }
    }
}
