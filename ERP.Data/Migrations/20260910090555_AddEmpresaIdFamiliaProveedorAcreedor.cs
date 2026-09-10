using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaIdFamiliaProveedorAcreedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Proveedores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Familia",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Acreedores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill existentes: asignar a primera empresa para no romper FK (0 no existe)
            migrationBuilder.Sql("UPDATE Familia SET EmpresaId = COALESCE((SELECT Id FROM Empresas ORDER BY Id LIMIT 1), 1) WHERE EmpresaId = 0;");
            migrationBuilder.Sql("UPDATE Proveedores SET EmpresaId = COALESCE((SELECT Id FROM Empresas ORDER BY Id LIMIT 1), 1) WHERE EmpresaId = 0;");
            migrationBuilder.Sql("UPDATE Acreedores SET EmpresaId = COALESCE((SELECT Id FROM Empresas ORDER BY Id LIMIT 1), 1) WHERE EmpresaId = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_EmpresaId",
                table: "Proveedores",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Familia_EmpresaId",
                table: "Familia",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Acreedores_EmpresaId",
                table: "Acreedores",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Acreedores_Empresas_EmpresaId",
                table: "Acreedores",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Familia_Empresas_EmpresaId",
                table: "Familia",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedores_Empresas_EmpresaId",
                table: "Proveedores",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Acreedores_Empresas_EmpresaId",
                table: "Acreedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Familia_Empresas_EmpresaId",
                table: "Familia");

            migrationBuilder.DropForeignKey(
                name: "FK_Proveedores_Empresas_EmpresaId",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Proveedores_EmpresaId",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Familia_EmpresaId",
                table: "Familia");

            migrationBuilder.DropIndex(
                name: "IX_Acreedores_EmpresaId",
                table: "Acreedores");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Familia");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Acreedores");
        }
    }
}
