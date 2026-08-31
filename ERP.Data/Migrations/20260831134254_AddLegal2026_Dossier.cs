using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegal2026_Dossier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseImponible",
                table: "RegistrosVerifactu",
                type: "decimal(18,2)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CodigoErrorAeat",
                table: "RegistrosVerifactu",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CuotaRecargoEquivalencia",
                table: "RegistrosVerifactu",
                type: "decimal(18,2)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionOperacion",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacturasRectificadasJson",
                table: "RegistrosVerifactu",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacturasSustituidasJson",
                table: "RegistrosVerifactu",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmaRegistroBase64",
                table: "RegistrosVerifactu",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdSistemaInformatico",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Incidencia",
                table: "RegistrosVerifactu",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NombreRazonEmisor",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreSistemaInformatico",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroInstalacion",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RechazoPrevio",
                table: "RegistrosVerifactu",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RefExterna",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoHuella",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoRectificativa",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionSistemaInformatico",
                table: "RegistrosVerifactu",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsAdministracionPublica",
                table: "Proveedores",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NIF_UE",
                table: "Proveedores",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaisISO",
                table: "Proveedores",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseContingenciasComunes",
                table: "Nominas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseContingenciasProfesionales",
                table: "Nominas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseHorasExtra",
                table: "Nominas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CodigoCuentaCotizacion",
                table: "Nominas",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CuotaSegSocialTrabajador",
                table: "Nominas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "GrupoCotizacion",
                table: "Nominas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "HorasExtra",
                table: "Nominas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagasExtraProrrateadas",
                table: "Nominas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RemesaSEPAId",
                table: "Nominas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RetencionIRPF",
                table: "Nominas",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TipoIRPF",
                table: "Nominas",
                type: "decimal(5,2)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CertificadoVerifactuId",
                table: "Empresas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsSII",
                table: "Empresas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAltaVerifactu",
                table: "Empresas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdSistemaInformatico",
                table: "Empresas",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModalidadVerifactu",
                table: "Empresas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NombreSistemaInformatico",
                table: "Empresas",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroInstalacion",
                table: "Empresas",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TerritorioFiscal",
                table: "Empresas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VersionSistemaInformatico",
                table: "Empresas",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CNAE",
                table: "Empleados",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoCuentaCotizacion",
                table: "Empleados",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConvenioColectivo",
                table: "Empleados",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAntiguedad",
                table: "Empleados",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GrupoCotizacion",
                table: "Empleados",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NumeroAfiliacionNAF",
                table: "Empleados",
                type: "TEXT",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Empleados",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsFacturaSimplificada",
                table: "Documentos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsFacturaSinIdentifDestinatario",
                table: "Documentos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FacturasRectificadasJson",
                table: "Documentos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncidenciaVerifactu",
                table: "Documentos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoRectificativa",
                table: "Documentos",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeRecargoEquivalencia",
                table: "DocumentoLineas",
                type: "decimal(5,2)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeRetencionIRPF",
                table: "DocumentoLineas",
                type: "decimal(5,2)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TipoIvaCatalogo",
                table: "DocumentoLineas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DireccionIP",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispositivoId",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "ControlesHorarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCorreccion",
                table: "ControlesHorarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "ControlesHorarios",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FirmaEmpleadoBase64",
                table: "ControlesHorarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Geolocalizacion",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HashAnterior",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Modalidad",
                table: "ControlesHorarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCorreccion",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Origen",
                table: "ControlesHorarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoRegistro",
                table: "ControlesHorarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCorreccion",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCreacion",
                table: "ControlesHorarios",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DIR3_OficinaContable",
                table: "Clientes",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DIR3_OrganoGestor",
                table: "Clientes",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DIR3_UnidadTramitadora",
                table: "Clientes",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsAdministracionPublica",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NIF_UE",
                table: "Clientes",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaisISO",
                table: "Clientes",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacturasElectronicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Formato = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    XmlBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FirmaXAdESBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    CertificadoId = table.Column<int>(type: "INTEGER", nullable: true),
                    DIR3_OficinaContable = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DIR3_OrganoGestor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DIR3_UnidadTramitadora = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PuntoEntrada = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaAcuseRecibo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CodigoRegistroFACe = table.Column<string>(type: "TEXT", nullable: true),
                    MensajeEstado = table.Column<string>(type: "TEXT", nullable: true),
                    PlataformaB2B = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AcuseReciboB2BJson = table.Column<string>(type: "TEXT", nullable: true),
                    FechaLimitePago = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasElectronicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturasElectronicas_CertificadosDigitales_CertificadoId",
                        column: x => x.CertificadoId,
                        principalTable: "CertificadosDigitales",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FacturasElectronicas_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturasElectronicas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LibrosRegistroIVA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoLibro = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaOperacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NumeroFactura = table.Column<string>(type: "TEXT", nullable: true),
                    NIFContraparte = table.Column<string>(type: "TEXT", nullable: true),
                    NombreContraparte = table.Column<string>(type: "TEXT", nullable: true),
                    BaseImponible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    TipoImpositivo = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    CuotaIVA = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CuotaRecargo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    EsDeducible = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClaveOperacion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrosRegistroIVA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibrosRegistroIVA_EjerciciosContables_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "EjerciciosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibrosRegistroIVA_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiquidacionesSeguridadSocial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoCuentaCotizacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Anio = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodoDesde = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodoHasta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    XmlRlcBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    XmlRntBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoRespuestaTGSS = table.Column<string>(type: "TEXT", nullable: true),
                    ImporteTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidacionesSeguridadSocial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidacionesSeguridadSocial_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoliticasControlHorario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiereGeolocalizacion = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermiteAutoCorreccion = table.Column<bool>(type: "INTEGER", nullable: false),
                    MargenToleranciaMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    HorasExtraMaxMes = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiereFirmaCorreccion = table.Column<bool>(type: "INTEGER", nullable: false),
                    AniosConservacion = table.Column<int>(type: "INTEGER", nullable: false),
                    ConvenioReferencia = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasControlHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoliticasControlHorario_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosTratamiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreTratamiento = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Finalidad = table.Column<string>(type: "TEXT", nullable: false),
                    BaseJuridica = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CategoriasInteresados = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CategoriasDatos = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Destinatarios = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PlazoConservacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MedidasTecnicas = table.Column<string>(type: "TEXT", nullable: true),
                    RequiereEIPD = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaEIPD = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TieneEncargadoTratamiento = table.Column<bool>(type: "INTEGER", nullable: false),
                    DelegadoProteccionDatos = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosTratamiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosTratamiento_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosVerifactuAnulacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegistroAltaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NifEmisor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NumeroFactura = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    FechaExpedicion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHoraHusoGeneracion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    HuellaAnterior = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Huella = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EstadoRemision = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaRemision = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IntentosRemision = table.Column<int>(type: "INTEGER", nullable: false),
                    RespuestaAeat = table.Column<string>(type: "TEXT", nullable: true),
                    DatosRegistroJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosVerifactuAnulacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosVerifactuAnulacion_RegistrosVerifactu_RegistroAltaId",
                        column: x => x.RegistroAltaId,
                        principalTable: "RegistrosVerifactu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarifasImpuesto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Territorio = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoIVA = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Porcentaje = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    RecargoEquivalencia = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifasImpuesto", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nominas_RemesaSEPAId",
                table: "Nominas",
                column: "RemesaSEPAId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_CertificadoVerifactuId",
                table: "Empresas",
                column: "CertificadoVerifactuId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_CertificadoId",
                table: "FacturasElectronicas",
                column: "CertificadoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_DocumentoId",
                table: "FacturasElectronicas",
                column: "DocumentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_EmpresaId",
                table: "FacturasElectronicas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosRegistroIVA_EjercicioId",
                table: "LibrosRegistroIVA",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosRegistroIVA_EmpresaId",
                table: "LibrosRegistroIVA",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionesSeguridadSocial_EmpresaId",
                table: "LiquidacionesSeguridadSocial",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticasControlHorario_EmpresaId",
                table: "PoliticasControlHorario",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosTratamiento_EmpresaId",
                table: "RegistrosTratamiento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosVerifactuAnulacion_RegistroAltaId",
                table: "RegistrosVerifactuAnulacion",
                column: "RegistroAltaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Empresas_CertificadosDigitales_CertificadoVerifactuId",
                table: "Empresas",
                column: "CertificadoVerifactuId",
                principalTable: "CertificadosDigitales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Nominas_RemesasSEPA_RemesaSEPAId",
                table: "Nominas",
                column: "RemesaSEPAId",
                principalTable: "RemesasSEPA",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empresas_CertificadosDigitales_CertificadoVerifactuId",
                table: "Empresas");

            migrationBuilder.DropForeignKey(
                name: "FK_Nominas_RemesasSEPA_RemesaSEPAId",
                table: "Nominas");

            migrationBuilder.DropTable(
                name: "FacturasElectronicas");

            migrationBuilder.DropTable(
                name: "LibrosRegistroIVA");

            migrationBuilder.DropTable(
                name: "LiquidacionesSeguridadSocial");

            migrationBuilder.DropTable(
                name: "PoliticasControlHorario");

            migrationBuilder.DropTable(
                name: "RegistrosTratamiento");

            migrationBuilder.DropTable(
                name: "RegistrosVerifactuAnulacion");

            migrationBuilder.DropTable(
                name: "TarifasImpuesto");

            migrationBuilder.DropIndex(
                name: "IX_Nominas_RemesaSEPAId",
                table: "Nominas");

            migrationBuilder.DropIndex(
                name: "IX_Empresas_CertificadoVerifactuId",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "BaseImponible",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "CodigoErrorAeat",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "CuotaRecargoEquivalencia",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "DescripcionOperacion",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "FacturasRectificadasJson",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "FacturasSustituidasJson",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "FirmaRegistroBase64",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "IdSistemaInformatico",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "Incidencia",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "NombreRazonEmisor",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "NombreSistemaInformatico",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "NumeroInstalacion",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "RechazoPrevio",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "RefExterna",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "TipoHuella",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "TipoRectificativa",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "VersionSistemaInformatico",
                table: "RegistrosVerifactu");

            migrationBuilder.DropColumn(
                name: "EsAdministracionPublica",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "NIF_UE",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "PaisISO",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "BaseContingenciasComunes",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "BaseContingenciasProfesionales",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "BaseHorasExtra",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "CodigoCuentaCotizacion",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "CuotaSegSocialTrabajador",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "GrupoCotizacion",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "HorasExtra",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "PagasExtraProrrateadas",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "RemesaSEPAId",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "RetencionIRPF",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "TipoIRPF",
                table: "Nominas");

            migrationBuilder.DropColumn(
                name: "CertificadoVerifactuId",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "EsSII",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "FechaAltaVerifactu",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "IdSistemaInformatico",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "ModalidadVerifactu",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "NombreSistemaInformatico",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "NumeroInstalacion",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "TerritorioFiscal",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "VersionSistemaInformatico",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "CNAE",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "CodigoCuentaCotizacion",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "ConvenioColectivo",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FechaAntiguedad",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "GrupoCotizacion",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "NumeroAfiliacionNAF",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "EsFacturaSimplificada",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "EsFacturaSinIdentifDestinatario",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "FacturasRectificadasJson",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "IncidenciaVerifactu",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "TipoRectificativa",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "PorcentajeRecargoEquivalencia",
                table: "DocumentoLineas");

            migrationBuilder.DropColumn(
                name: "PorcentajeRetencionIRPF",
                table: "DocumentoLineas");

            migrationBuilder.DropColumn(
                name: "TipoIvaCatalogo",
                table: "DocumentoLineas");

            migrationBuilder.DropColumn(
                name: "DireccionIP",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "DispositivoId",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "FechaCorreccion",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "FirmaEmpleadoBase64",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "Geolocalizacion",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "HashAnterior",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "MotivoCorreccion",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "TipoRegistro",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "UsuarioCorreccion",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "UsuarioCreacion",
                table: "ControlesHorarios");

            migrationBuilder.DropColumn(
                name: "DIR3_OficinaContable",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "DIR3_OrganoGestor",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "DIR3_UnidadTramitadora",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "EsAdministracionPublica",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "NIF_UE",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "PaisISO",
                table: "Clientes");
        }
    }
}
