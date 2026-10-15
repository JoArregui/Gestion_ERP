using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseC_Compras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BloqueadoCompras",
                table: "Proveedores",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CertificadoCalidad",
                table: "Proveedores",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluacionCalidad",
                table: "Proveedores",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluacionPlazo",
                table: "Proveedores",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaHomologacion",
                table: "Proveedores",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Homologado",
                table: "Proveedores",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivoBloqueo",
                table: "Proveedores",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SolicitudesOferta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaLimite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProveedoresInvitadosJson = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PedidoAdjudicadoId = table.Column<int>(type: "INTEGER", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesOferta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesOferta_Documentos_PedidoAdjudicadoId",
                        column: x => x.PedidoAdjudicadoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesOferta_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineasSolicitudOferta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasSolicitudOferta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasSolicitudOferta_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasSolicitudOferta_SolicitudesOferta_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "SolicitudesOferta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfertasProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    PlazoEntregaDias = table.Column<int>(type: "INTEGER", nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfertasProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfertasProveedor_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfertasProveedor_SolicitudesOferta_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "SolicitudesOferta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineasOferta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OfertaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasOferta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasOferta_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasOferta_OfertasProveedor_OfertaId",
                        column: x => x.OfertaId,
                        principalTable: "OfertasProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineasOferta_ArticuloId",
                table: "LineasOferta",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasOferta_OfertaId",
                table: "LineasOferta",
                column: "OfertaId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasSolicitudOferta_ArticuloId",
                table: "LineasSolicitudOferta",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasSolicitudOferta_SolicitudId",
                table: "LineasSolicitudOferta",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_OfertasProveedor_ProveedorId",
                table: "OfertasProveedor",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfertasProveedor_SolicitudId",
                table: "OfertasProveedor",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesOferta_EmpresaId",
                table: "SolicitudesOferta",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesOferta_PedidoAdjudicadoId",
                table: "SolicitudesOferta",
                column: "PedidoAdjudicadoId");

            // Fase C2: proveedores preexistentes quedan homologados (los nuevos nacen sin homologar)
            migrationBuilder.Sql("UPDATE Proveedores SET Homologado = 1 WHERE IsActivo = 1 AND (BloqueadoCompras IS NULL OR BloqueadoCompras = 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasOferta");

            migrationBuilder.DropTable(
                name: "LineasSolicitudOferta");

            migrationBuilder.DropTable(
                name: "OfertasProveedor");

            migrationBuilder.DropTable(
                name: "SolicitudesOferta");

            migrationBuilder.DropColumn(
                name: "BloqueadoCompras",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "CertificadoCalidad",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "EvaluacionCalidad",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "EvaluacionPlazo",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "FechaHomologacion",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Homologado",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "MotivoBloqueo",
                table: "Proveedores");
        }
    }
}
