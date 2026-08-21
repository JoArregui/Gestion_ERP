using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifactuRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documentos_EmpresaId",
                table: "Documentos");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_EmpresaId_NumeroDocumento",
                table: "Documentos",
                columns: new[] { "EmpresaId", "NumeroDocumento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documentos_EmpresaId_NumeroDocumento",
                table: "Documentos");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_EmpresaId",
                table: "Documentos",
                column: "EmpresaId");
        }
    }
}
