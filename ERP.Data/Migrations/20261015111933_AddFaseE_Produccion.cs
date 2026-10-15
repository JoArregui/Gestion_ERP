using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseE_Produccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BOMLineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticuloPadreId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SustitutoComponenteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOMLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BOMLineas_Articulos_ArticuloPadreId",
                        column: x => x.ArticuloPadreId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BOMLineas_Articulos_ComponenteId",
                        column: x => x.ComponenteId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BOMLineas_Articulos_SustitutoComponenteId",
                        column: x => x.SustitutoComponenteId,
                        principalTable: "Articulos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BOMLineas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CentrosTrabajo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    CosteHoraMaquina = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CosteHoraManoObra = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CapacidadDiariaMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CentrosTrabajo_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesFabricacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroOF = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadPlanificada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadTerminada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadRechazada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaPlanificadaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaPlanificadaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaRealInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaRealFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CosteMaterialTeorico = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteMaterialReal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteManoObraReal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteMaquinaReal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteIndirectoReal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesFabricacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesFabricacion_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesFabricacion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HojasRuta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroOperacion = table.Column<int>(type: "INTEGER", nullable: false),
                    CentroTrabajoId = table.Column<int>(type: "INTEGER", nullable: false),
                    DescripcionOperacion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TiempoPreparacionMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    TiempoPorUnidadMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CentroCosteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HojasRuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HojasRuta_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HojasRuta_CentrosTrabajo_CentroTrabajoId",
                        column: x => x.CentroTrabajoId,
                        principalTable: "CentrosTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HojasRuta_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OFMateriales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdenFabricacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadTeorica = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadReal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LoteId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OFMateriales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OFMateriales_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OFMateriales_OrdenesFabricacion_OrdenFabricacionId",
                        column: x => x.OrdenFabricacionId,
                        principalTable: "OrdenesFabricacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OFOperaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdenFabricacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    HojaRutaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CentroTrabajoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroOperacion = table.Column<int>(type: "INTEGER", nullable: false),
                    TiempoPreparacionRealMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    TiempoUnidadRealMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnidadesProducidas = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteManoObra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteMaquina = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OperarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OFOperaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OFOperaciones_CentrosTrabajo_CentroTrabajoId",
                        column: x => x.CentroTrabajoId,
                        principalTable: "CentrosTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OFOperaciones_HojasRuta_HojaRutaId",
                        column: x => x.HojaRutaId,
                        principalTable: "HojasRuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OFOperaciones_OrdenesFabricacion_OrdenFabricacionId",
                        column: x => x.OrdenFabricacionId,
                        principalTable: "OrdenesFabricacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BOMLineas_ArticuloPadreId_ComponenteId_Version",
                table: "BOMLineas",
                columns: new[] { "ArticuloPadreId", "ComponenteId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BOMLineas_ComponenteId",
                table: "BOMLineas",
                column: "ComponenteId");

            migrationBuilder.CreateIndex(
                name: "IX_BOMLineas_EmpresaId",
                table: "BOMLineas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_BOMLineas_SustitutoComponenteId",
                table: "BOMLineas",
                column: "SustitutoComponenteId");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosTrabajo_EmpresaId",
                table: "CentrosTrabajo",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_HojasRuta_ArticuloId_NumeroOperacion_Version",
                table: "HojasRuta",
                columns: new[] { "ArticuloId", "NumeroOperacion", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HojasRuta_CentroTrabajoId",
                table: "HojasRuta",
                column: "CentroTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_HojasRuta_EmpresaId",
                table: "HojasRuta",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_OFMateriales_ArticuloId",
                table: "OFMateriales",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_OFMateriales_OrdenFabricacionId",
                table: "OFMateriales",
                column: "OrdenFabricacionId");

            migrationBuilder.CreateIndex(
                name: "IX_OFOperaciones_CentroTrabajoId",
                table: "OFOperaciones",
                column: "CentroTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_OFOperaciones_HojaRutaId",
                table: "OFOperaciones",
                column: "HojaRutaId");

            migrationBuilder.CreateIndex(
                name: "IX_OFOperaciones_OrdenFabricacionId",
                table: "OFOperaciones",
                column: "OrdenFabricacionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesFabricacion_ArticuloId",
                table: "OrdenesFabricacion",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesFabricacion_EmpresaId",
                table: "OrdenesFabricacion",
                column: "EmpresaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BOMLineas");

            migrationBuilder.DropTable(
                name: "OFMateriales");

            migrationBuilder.DropTable(
                name: "OFOperaciones");

            migrationBuilder.DropTable(
                name: "HojasRuta");

            migrationBuilder.DropTable(
                name: "OrdenesFabricacion");

            migrationBuilder.DropTable(
                name: "CentrosTrabajo");
        }
    }
}
