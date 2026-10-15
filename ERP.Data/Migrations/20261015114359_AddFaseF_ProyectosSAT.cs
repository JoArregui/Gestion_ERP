using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaseF_ProyectosSAT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContratosMantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SLA = table.Column<int>(type: "INTEGER", nullable: false),
                    CuotaAnual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DescripcionCobertura = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosMantenimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContratosMantenimiento_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratosMantenimiento_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaInicioPlanificada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFinPlanificada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaInicioReal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFinReal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    PresupuestoCosteTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PresupuestoVentaTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteRealAcumulado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FacturadoAcumulado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ResponsableId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Proyectos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TareaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaSubida = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosProyecto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosProyecto_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HitosProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicioPlanificada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFinPlanificada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaInicioReal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFinReal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    PorcentajeAvance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HitosProyecto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HitosProyecto_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketsSAT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContratoMantenimientoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroTicket = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Asunto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Prioridad = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    TecnicoAsignadoId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaApertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaInicioTrabajo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SLAFechaLimiteRespuesta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SLAFechaLimiteResolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SLAIncumplido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Solucion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    HorasTrabajadas = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteMateriales = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteDesplazamiento = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketsSAT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketsSAT_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketsSAT_ContratosMantenimiento_ContratoMantenimientoId",
                        column: x => x.ContratoMantenimientoId,
                        principalTable: "ContratosMantenimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketsSAT_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketsSAT_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TareasProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: false),
                    HitoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicioPlanificada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFinPlanificada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaInicioReal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFinReal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    PorcentajeAvance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    HorasEstimadas = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HorasReales = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteHoraEstimado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteMaterialEstimado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteRealHoras = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteRealMaterial = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TareaPredecesoraId = table.Column<int>(type: "INTEGER", nullable: true),
                    ResponsableId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasProyecto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TareasProyecto_HitosProyecto_HitoId",
                        column: x => x.HitoId,
                        principalTable: "HitosProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TareasProyecto_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TareasProyecto_TareasProyecto_TareaPredecesoraId",
                        column: x => x.TareaPredecesoraId,
                        principalTable: "TareasProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketComentarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    EsInterno = table.Column<bool>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketComentarios_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketComentarios_TicketsSAT_TicketId",
                        column: x => x.TicketId,
                        principalTable: "TicketsSAT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetsProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TareaId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Horas = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CosteHora = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Facturable = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetsProyecto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetsProyecto_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetsProyecto_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimesheetsProyecto_TareasProyecto_TareaId",
                        column: x => x.TareaId,
                        principalTable: "TareasProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContratosMantenimiento_ClienteId",
                table: "ContratosMantenimiento",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosMantenimiento_EmpresaId",
                table: "ContratosMantenimiento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosProyecto_ProyectoId",
                table: "DocumentosProyecto",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_HitosProyecto_ProyectoId",
                table: "HitosProyecto",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_ClienteId",
                table: "Proyectos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_EmpresaId_Codigo",
                table: "Proyectos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TareasProyecto_HitoId",
                table: "TareasProyecto",
                column: "HitoId");

            migrationBuilder.CreateIndex(
                name: "IX_TareasProyecto_ProyectoId",
                table: "TareasProyecto",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_TareasProyecto_TareaPredecesoraId",
                table: "TareasProyecto",
                column: "TareaPredecesoraId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComentarios_EmpleadoId",
                table: "TicketComentarios",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComentarios_TicketId",
                table: "TicketComentarios",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAT_ClienteId",
                table: "TicketsSAT",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAT_ContratoMantenimientoId",
                table: "TicketsSAT",
                column: "ContratoMantenimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAT_EmpresaId_NumeroTicket",
                table: "TicketsSAT",
                columns: new[] { "EmpresaId", "NumeroTicket" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAT_ProyectoId",
                table: "TicketsSAT",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetsProyecto_EmpleadoId",
                table: "TimesheetsProyecto",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetsProyecto_ProyectoId",
                table: "TimesheetsProyecto",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetsProyecto_TareaId",
                table: "TimesheetsProyecto",
                column: "TareaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosProyecto");

            migrationBuilder.DropTable(
                name: "TicketComentarios");

            migrationBuilder.DropTable(
                name: "TimesheetsProyecto");

            migrationBuilder.DropTable(
                name: "TicketsSAT");

            migrationBuilder.DropTable(
                name: "TareasProyecto");

            migrationBuilder.DropTable(
                name: "ContratosMantenimiento");

            migrationBuilder.DropTable(
                name: "HitosProyecto");

            migrationBuilder.DropTable(
                name: "Proyectos");
        }
    }
}
