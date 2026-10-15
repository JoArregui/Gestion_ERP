using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseG_AuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Entidad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PropsIgnoradas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PropsIncluidas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AuditarInsert = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuditarUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuditarDelete = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Entidad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntidadId = table.Column<int>(type: "INTEGER", nullable: false),
                    Accion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioNombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ip = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ValoresAntes = table.Column<string>(type: "TEXT", nullable: true),
                    ValoresDespues = table.Column<string>(type: "TEXT", nullable: true),
                    Diferencia = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditConfigs_EmpresaId_Entidad",
                table: "AuditConfigs",
                columns: new[] { "EmpresaId", "Entidad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EmpresaId_Entidad_EntidadId",
                table: "AuditLogs",
                columns: new[] { "EmpresaId", "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Fecha",
                table: "AuditLogs",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditConfigs");

            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}
