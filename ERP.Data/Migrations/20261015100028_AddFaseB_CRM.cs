using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseB_CRM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    EmpresaNombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    CIF = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Origen = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioAsignado = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaContacto = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaConversion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Oportunidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LeadId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ImporteEstimado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Probabilidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Etapa = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCierrePrevista = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MotivoPerdida = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DocumentoPresupuestoId = table.Column<int>(type: "INTEGER", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oportunidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Oportunidades_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oportunidades_Documentos_DocumentoPresupuestoId",
                        column: x => x.DocumentoPresupuestoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oportunidades_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oportunidades_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ClienteId",
                table: "Leads",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_EmpresaId",
                table: "Leads",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Oportunidades_ClienteId",
                table: "Oportunidades",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Oportunidades_DocumentoPresupuestoId",
                table: "Oportunidades",
                column: "DocumentoPresupuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_Oportunidades_EmpresaId",
                table: "Oportunidades",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Oportunidades_LeadId",
                table: "Oportunidades",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Oportunidades");

            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
