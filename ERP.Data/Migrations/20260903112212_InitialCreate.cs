using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Acreedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CIF = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    NombreContacto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EsAcreedor = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acreedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesGenerales",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    UltimaModificacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesGenerales", x => x.Clave);
                });

            migrationBuilder.CreateTable(
                name: "Familia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CodigoInterno = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IsActiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Familia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CIF = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    NombreContacto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EsAcreedor = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    NIF_UE = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PaisISO = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    EsAdministracionPublica = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioCompra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PorcentajeIva = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    Stock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockReservado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockMinimo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProveedorHabitualId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsDescatalogado = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImagenUrl = table.Column<string>(type: "TEXT", nullable: true),
                    FamiliaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articulos_Familia_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Articulos_Proveedores_ProveedorHabitualId",
                        column: x => x.ProveedorHabitualId,
                        principalTable: "Proveedores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AlertasTrazabilidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Severidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaAlerta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Leida = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaLectura = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioLectura = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Resuelta = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioResolucion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AccionCorrectiva = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasTrazabilidad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApuntesContables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AsientoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaContableCodigo = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CentroCosteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: true),
                    DocumentoReferencia = table.Column<string>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApuntesContables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AsientosContables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Serie = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDebe = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalHaber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrigenTipo = table.Column<string>(type: "TEXT", nullable: true),
                    OrigenId = table.Column<int>(type: "INTEGER", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioContabilizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaContabilizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LibroDiarioId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientosContables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    IsActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificadosDigitales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectDN = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IssuerDN = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NotBefore = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotAfter = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ThumbprintSHA256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ThumbprintSHA1 = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PublicKeyPem = table.Column<string>(type: "TEXT", nullable: false),
                    AlgoritmoFirma = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AlgoritmoClave = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TamanoClave = table.Column<int>(type: "INTEGER", nullable: false),
                    Uso = table.Column<int>(type: "INTEGER", nullable: false),
                    Almacenamiento = table.Column<int>(type: "INTEGER", nullable: false),
                    QTSP = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroAutorizacionQTSP = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PoliticaFirmaOID = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CadenaCertificadosPem = table.Column<string>(type: "TEXT", nullable: true),
                    OCSPUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CRLUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Revocado = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaRevoca = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MotivoRevoca = table.Column<string>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificadosDigitales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreComercial = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CIF = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoPostal = table.Column<string>(type: "TEXT", nullable: true),
                    Poblacion = table.Column<string>(type: "TEXT", nullable: true),
                    Provincia = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", nullable: true),
                    Web = table.Column<string>(type: "TEXT", nullable: true),
                    RegistroMercantil = table.Column<string>(type: "TEXT", nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LogoBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: false),
                    Eslogan = table.Column<string>(type: "TEXT", nullable: true),
                    SerieFacturacion = table.Column<string>(type: "TEXT", nullable: false),
                    UltimoNumeroFactura = table.Column<int>(type: "INTEGER", nullable: false),
                    IvaDefecto = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModalidadVerifactu = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaAltaVerifactu = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CertificadoVerifactuId = table.Column<int>(type: "INTEGER", nullable: true),
                    NombreSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VersionSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IdSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroInstalacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TerritorioFiscal = table.Column<int>(type: "INTEGER", nullable: false),
                    EsSII = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empresas_CertificadosDigitales_CertificadoVerifactuId",
                        column: x => x.CertificadoVerifactuId,
                        principalTable: "CertificadosDigitales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CierresCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Terminal = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalVentasEfectivo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalVentasTarjeta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Base21 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Iva21 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Base10 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Iva10 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Base4 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Iva4 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalIva = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ImporteRealEnCaja = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", nullable: true),
                    IsProcesado = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCategoriasJson = table.Column<string>(type: "TEXT", nullable: true),
                    DataUsuariosJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CierresCaja_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodigoCliente = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    NombreComercial = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CIF = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoPostal = table.Column<string>(type: "TEXT", nullable: true),
                    Poblacion = table.Column<string>(type: "TEXT", nullable: true),
                    Provincia = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", nullable: true),
                    TieneRecargoEquivalencia = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiaPagoHabitual = table.Column<int>(type: "INTEGER", nullable: false),
                    FormaPago = table.Column<string>(type: "TEXT", nullable: true),
                    DescuentoFijo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IsActivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBloqueado = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoBloqueo = table.Column<string>(type: "TEXT", nullable: true),
                    EsAdministracionPublica = table.Column<bool>(type: "INTEGER", nullable: false),
                    DIR3_OficinaContable = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DIR3_OrganoGestor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DIR3_UnidadTramitadora = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NIF_UE = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PaisISO = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clientes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunicacionesCertificadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Asunto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Contenido = table.Column<string>(type: "TEXT", nullable: false),
                    RemitenteNombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RemitenteEmail = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DestinatariosJson = table.Column<string>(type: "TEXT", nullable: false),
                    AdjuntosJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequiereAcuseRecibo = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiereEntregaPersonal = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaLimiteEntrega = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaAcuseRecibo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PruebaEntregaBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    PruebaContenidoBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunicacionesCertificadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunicacionesCertificadas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CuentasBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    IBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    BIC = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    NombreCuenta = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntidadBancaria = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CodigoEntidad = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    CodigoOficina = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    DigitosControl = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    NumeroCuenta = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreditorIdentifier = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    PermiteTransferenciasSEPA = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermiteAdeudosSEPA = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermiteTransferenciasInstant = table.Column<bool>(type: "INTEGER", nullable: false),
                    LimiteDiarioTransferencias = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: true),
                    LimiteDiarioAdeudos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasBancarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasBancarias_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CuentasContables",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Grupo = table.Column<int>(type: "INTEGER", nullable: false),
                    Nivel = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoPadre = table.Column<string>(type: "TEXT", nullable: true),
                    EsDetalle = table.Column<bool>(type: "INTEGER", nullable: false),
                    Naturaleza = table.Column<int>(type: "INTEGER", nullable: false),
                    EsPGCOficial = table.Column<bool>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasContables", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_CuentasContables_CuentasContables_CodigoPadre",
                        column: x => x.CodigoPadre,
                        principalTable: "CuentasContables",
                        principalColumn: "Codigo");
                    table.ForeignKey(
                        name: "FK_CuentasContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EjerciciosContables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCierre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CierreTrimestral1 = table.Column<bool>(type: "INTEGER", nullable: false),
                    CierreTrimestral2 = table.Column<bool>(type: "INTEGER", nullable: false),
                    CierreTrimestral3 = table.Column<bool>(type: "INTEGER", nullable: false),
                    CierreTrimestral4 = table.Column<bool>(type: "INTEGER", nullable: false),
                    LibrosLegalizados = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaLegalizacionLibros = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CuentasDepositadas = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaDepositoCuentas = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NumeroDeposito = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaInicioActividad = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CierreDefinitivo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjerciciosContables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EjerciciosContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DNI = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", nullable: true),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CCC = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NumeroSeguridadSocial = table.Column<string>(type: "TEXT", nullable: false),
                    PinAcceso = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Cargo = table.Column<string>(type: "TEXT", nullable: true),
                    Departamento = table.Column<string>(type: "TEXT", nullable: true),
                    GrupoCotizacion = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoCuentaCotizacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ConvenioColectivo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TipoContrato = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CNAE = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NumeroAfiliacionNAF = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    FechaAntiguedad = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SalarioBrutoAnual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SalarioBaseMensual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: true),
                    VacacionesTotales = table.Column<int>(type: "INTEGER", nullable: false),
                    VacacionesDisfrutadas = table.Column<int>(type: "INTEGER", nullable: false),
                    RutaDocumentoPdf = table.Column<string>(type: "TEXT", nullable: true),
                    NombreArchivoPdf = table.Column<string>(type: "TEXT", nullable: true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleados_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FirmasElectronicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CertificadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Formato = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentoTipo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentoReferencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HashDocumentoSHA256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HashDocumentoSHA1 = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    FirmaBase64 = table.Column<string>(type: "TEXT", nullable: false),
                    FirmaEstructurada = table.Column<string>(type: "TEXT", nullable: false),
                    FechaFirma = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TSPUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TimestampTokenBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    PoliticaFirmaOID = table.Column<string>(type: "TEXT", nullable: true),
                    FirmanteNombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FirmanteNIF = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirmanteCargo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FirmanteEmail = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DireccionIP = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Geolocalizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EstadoVerificacion = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaUltimaVerificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DetalleVerificacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ArchivoFirmadoBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    NombreArchivoFirmado = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmasElectronicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirmasElectronicas_CertificadosDigitales_CertificadoId",
                        column: x => x.CertificadoId,
                        principalTable: "CertificadosDigitales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FirmasElectronicas_Empresas_EmpresaId",
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
                name: "Llamadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Empresa = table.Column<string>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", nullable: false),
                    Tiempo = table.Column<string>(type: "TEXT", nullable: false),
                    Urgente = table.Column<bool>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Llamadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Llamadas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LotesTrazabilidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoLote = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: true),
                    LoteProveedor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DocumentoOrigen = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CantidadInicial = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadActual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadReservada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    FechaProduccion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCaducidad = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaConsumoPreferente = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CondicionesAlmacenamiento = table.Column<string>(type: "TEXT", nullable: true),
                    TemperaturaMinima = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    TemperaturaMaxima = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    TieneAnalisisOficial = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumeroCertificadoAnalisis = table.Column<string>(type: "TEXT", nullable: true),
                    FechaAnalisis = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LaboratorioAnalisis = table.Column<string>(type: "TEXT", nullable: true),
                    EsPCC = table.Column<bool>(type: "INTEGER", nullable: false),
                    PuntoControlCriticoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParametrosCriticos = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesTrazabilidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotesTrazabilidad_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LotesTrazabilidad_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LotesTrazabilidad_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockResultante = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReferenciaDocumento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Empresas_EmpresaId",
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
                    TransferenciasInternacionales = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaisDestino = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MecanismoSeguridad = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MedidasTecnicas = table.Column<string>(type: "TEXT", nullable: true),
                    RequiereEIPD = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaEIPD = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaProximaRevision = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaUltimaRevision = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TieneEncargadoTratamiento = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailDelegado = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TelefonoDelegado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DelegadoProteccionDatos = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EsTratamientoOcasional = table.Column<bool>(type: "INTEGER", nullable: false),
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
                name: "SectoresDiferenciadosIVA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PorcentajeDeduccion = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    VolumenOperacionesAnual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectoresDiferenciadosIVA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectoresDiferenciadosIVA_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellosTiempo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    HashDatosSHA256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TokenBase64 = table.Column<string>(type: "TEXT", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TSAUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TSAName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TSACertThumbprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    PoliticaTSA_OID = table.Column<string>(type: "TEXT", nullable: true),
                    ReferenciaDocumento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellosTiempo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellosTiempo_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hora = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Prioridad = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Completada = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tareas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tareas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    EsCompra = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumeroAlbaran = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroFacturaProveedor = table.Column<string>(type: "TEXT", nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: true),
                    DocumentoOrigenId = table.Column<int>(type: "INTEGER", nullable: true),
                    BaseImponible = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalIva = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsContabilizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncidenciaVerifactu = table.Column<bool>(type: "INTEGER", nullable: false),
                    EsFacturaSimplificada = table.Column<bool>(type: "INTEGER", nullable: false),
                    EsFacturaSinIdentifDestinatario = table.Column<bool>(type: "INTEGER", nullable: false),
                    TipoRectificativa = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    FacturasRectificadasJson = table.Column<string>(type: "TEXT", nullable: true),
                    EnviadaCliente = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaEnvioCliente = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PresentadaHacienda = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaPresentacionHacienda = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    MetodoPago = table.Column<string>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", nullable: true),
                    UsuarioNombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NotasInternas = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documentos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Documentos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtractosBancarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CuentaBancariaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenciaExtracto = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    FechaExtracto = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaValor = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    SaldoFinal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    TotalCargos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    TotalAbonos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    XmlOriginal = table.Column<string>(type: "TEXT", nullable: true),
                    HashXmlSHA256 = table.Column<string>(type: "TEXT", nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Procesado = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaProcesado = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractosBancarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractosBancarios_CuentasBancarias_CuentaBancariaId",
                        column: x => x.CuentaBancariaId,
                        principalTable: "CuentasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MandatosSEPA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenciaUnicaMandato = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    CreditorIdentifier = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    DeudorNombre = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    DeudorNombreComercial = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    DeudorCalle = table.Column<string>(type: "TEXT", maxLength: 70, nullable: true),
                    DeudorNumero = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    DeudorCodigoPostal = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    DeudorPoblacion = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    DeudorPais = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    DeudorIBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    DeudorBIC = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    DeudorIdentificacionFiscal = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AcreedorNombre = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    AcreedorNombreComercial = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    AcreedorCalle = table.Column<string>(type: "TEXT", maxLength: 70, nullable: true),
                    AcreedorNumero = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    AcreedorCodigoPostal = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    AcreedorPoblacion = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    AcreedorPais = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    AcreedorIBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    AcreedorBIC = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    AcreedorCreditorIdentifier = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    Esquema = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoSecuencia = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaFirma = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaPrimeraPresentacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaUltimaPresentacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    BeneficiarioVerificado = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaVerificacionBeneficiario = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MetodoVerificacion = table.Column<string>(type: "TEXT", nullable: true),
                    InformacionAdicional = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: true),
                    CuentaBancariaAcreedorId = table.Column<int>(type: "INTEGER", nullable: true),
                    CuentaBancariaDeudorId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatosSEPA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MandatosSEPA_CuentasBancarias_CuentaBancariaAcreedorId",
                        column: x => x.CuentaBancariaAcreedorId,
                        principalTable: "CuentasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MandatosSEPA_CuentasBancarias_CuentaBancariaDeudorId",
                        column: x => x.CuentaBancariaDeudorId,
                        principalTable: "CuentasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MandatosSEPA_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesIVA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AplicaProrrataGeneral = table.Column<bool>(type: "INTEGER", nullable: false),
                    PorcentajeProrrataGeneral = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    PorcentajeProrrataGeneralRedondeado = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: true),
                    AplicaProrrataEspecial = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetalleProrrataEspecialJson = table.Column<string>(type: "TEXT", nullable: true),
                    TieneSectoresDiferenciados = table.Column<bool>(type: "INTEGER", nullable: false),
                    SectoresJson = table.Column<string>(type: "TEXT", nullable: true),
                    SujetoRecargoEquivalencia = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecargoGeneral = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    RecargoReducido = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    RecargoSuperreducido = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    RecargoTabaco = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    RegimenAgenciasViajes = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegimenBienesUsados = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegimenObjetosArte = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegimenOroInversion = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegimenServiciosElectronicos = table.Column<bool>(type: "INTEGER", nullable: false),
                    AplicaIVACaja = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaInicioIVACaja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaFinIVACaja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LimiteVolumenOperacionesIVACaja = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: true),
                    InversionSujetoPasivoHabitual = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoliquidacionRectificativaAnual = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VersionConfig = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesIVA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesIVA_EjerciciosContables_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "EjerciciosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesIVA_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibrosDiario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioGeneracion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaLegalizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NumeroLegalizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HashArchivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NumeroLibro = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FirmaXAdESBase64 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FechaPresentacionRM = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalDebe = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalHaber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NumeroAsientos = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroApuntes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrosDiario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibrosDiario_EjerciciosContables_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "EjerciciosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibrosDiario_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibrosInventariosCuentasAnuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioGeneracion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaLegalizacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NumeroLegalizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HashArchivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaPresentacionRM = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BalanceInicialJson = table.Column<string>(type: "TEXT", nullable: true),
                    BalancesComprobacionTrimestralesJson = table.Column<string>(type: "TEXT", nullable: true),
                    InventarioCierreJson = table.Column<string>(type: "TEXT", nullable: true),
                    BalanceSituacionJson = table.Column<string>(type: "TEXT", nullable: true),
                    CuentaPerdidasGananciasJson = table.Column<string>(type: "TEXT", nullable: true),
                    EstadoCambiosPatrimonioNetJson = table.Column<string>(type: "TEXT", nullable: true),
                    EstadoFlujosEfectivoJson = table.Column<string>(type: "TEXT", nullable: true),
                    MemoriaJson = table.Column<string>(type: "TEXT", nullable: true),
                    EsAbreviado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrosInventariosCuentasAnuales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibrosInventariosCuentasAnuales_EjerciciosContables_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "EjerciciosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibrosInventariosCuentasAnuales_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibrosMayor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaContableCodigo = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    SaldoInicialDebe = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SaldoInicialHaber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalDebe = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalHaber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SaldoFinalDebe = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SaldoFinalHaber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NumeroApuntes = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrosMayor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibrosMayor_CuentasContables_CuentaContableCodigo",
                        column: x => x.CuentaContableCodigo,
                        principalTable: "CuentasContables",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibrosMayor_EjerciciosContables_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "EjerciciosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibrosMayor_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "LiquidacionesIVA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EjercicioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Periodo = table.Column<int>(type: "INTEGER", nullable: false),
                    Año = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaPresentacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modelo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    EsLiquidacionDefinitiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    Presentado = table.Column<bool>(type: "INTEGER", nullable: false),
                    BaseGeneral = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    BaseReducida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    BaseSuperreducida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    BaseExenta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    BaseNoSujeta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    BaseInversionSujetoPasivo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAGeneralRepercutido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAReducidoRepercutido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVASuperreducidoRepercutido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVARecargoEquivalencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAGeneralSoportado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAReducidoSoportado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVASuperreducidoSoportado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVARecargoEquivalenciaSoportado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    PorcentajeProrrataAplicada = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    UsaProrrataEspecial = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetalleProrrataAplicadaJson = table.Column<string>(type: "TEXT", nullable: true),
                    UsaSectoresDiferenciados = table.Column<bool>(type: "INTEGER", nullable: false),
                    DetalleSectoresJson = table.Column<string>(type: "TEXT", nullable: true),
                    IVADeducibleGeneral = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVADeducibleReducido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVADeducibleSuperreducido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVADeducibleTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVADevengadoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAAIngresar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVACompensar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVADevolver = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAAgenciasViajes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVABienesUsados = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAObjetosArte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAOroInversion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVAServiciosElectronicos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    IVACaja = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    RegularizacionAnual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    FechaCalculo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReferenciaPresentacion = table.Column<string>(type: "TEXT", nullable: true),
                    UsuarioCalculo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidacionesIVA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidacionesIVA_EjerciciosContables_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "EjerciciosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiquidacionesIVA_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlesHorarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Entrada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Salida = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Ubicacion = table.Column<string>(type: "TEXT", nullable: true),
                    TipoRegistro = table.Column<int>(type: "INTEGER", nullable: false),
                    Modalidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Origen = table.Column<int>(type: "INTEGER", nullable: false),
                    DireccionIP = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    Geolocalizacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DispositivoId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HashAnterior = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FirmaEmpleadoBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    FirmaValidada = table.Column<bool>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoCorreccion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UsuarioCorreccion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCorreccion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    JornadaCompleta = table.Column<bool>(type: "INTEGER", nullable: false),
                    HorasTotales = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFinConservacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlesHorarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlesHorarios_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesFirma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DocumentoBase64 = table.Column<string>(type: "TEXT", nullable: false),
                    NombreArchivo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HashDocumentoSHA256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FirmantesJson = table.Column<string>(type: "TEXT", nullable: false),
                    TipoFirmaRequerida = table.Column<int>(type: "INTEGER", nullable: false),
                    FormatoSalida = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordatoriosEnviados = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoRecordatorio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FirmaElectronicaId = table.Column<int>(type: "INTEGER", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesFirma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesFirma_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesFirma_FirmasElectronicas_FirmaElectronicaId",
                        column: x => x.FirmaElectronicaId,
                        principalTable: "FirmasElectronicas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MovimientosLote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: true),
                    LoteOrigenId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlmacenOrigenId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlmacenDestinoId = table.Column<int>(type: "INTEGER", nullable: true),
                    TipoDocumento = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    NumeroDocumento = table.Column<string>(type: "TEXT", nullable: true),
                    Transportista = table.Column<string>(type: "TEXT", nullable: true),
                    MatriculaVehiculo = table.Column<string>(type: "TEXT", nullable: true),
                    TemperaturaTransporte = table.Column<string>(type: "TEXT", nullable: true),
                    FechaSalidaTransporte = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaEntrega = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Responsable = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LoteResultadoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParametrosControlJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosLote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosLote_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MovimientosLote_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimientosLote_LotesTrazabilidad_LoteId",
                        column: x => x.LoteId,
                        principalTable: "LotesTrazabilidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosLote_LotesTrazabilidad_LoteOrigenId",
                        column: x => x.LoteOrigenId,
                        principalTable: "LotesTrazabilidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosLote_LotesTrazabilidad_LoteResultadoId",
                        column: x => x.LoteResultadoId,
                        principalTable: "LotesTrazabilidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosLote_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RetiradasLote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Severidad = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaDeteccion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaNotificacionAutoridades = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NumeroExpedienteAESAN = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroNotificacionRASFF = table.Column<string>(type: "TEXT", nullable: true),
                    CantidadAfectada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadRetirada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadDestruida = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ClientesAfectados = table.Column<int>(type: "INTEGER", nullable: true),
                    NotificadosClientes = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaNotificacionClientes = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NotificadosProveedores = table.Column<bool>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioResponsable = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AccionesCorrectivas = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetiradasLote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetiradasLote_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetiradasLote_LotesTrazabilidad_LoteId",
                        column: x => x.LoteId,
                        principalTable: "LotesTrazabilidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentoLineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticuloId = table.Column<int>(type: "INTEGER", nullable: true),
                    DescripcionArticulo = table.Column<string>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PorcentajeIva = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    PorcentajeRecargoEquivalencia = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    PorcentajeRetencionIRPF = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    TipoIvaCatalogo = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoriaNombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoLineas_Articulos_ArticuloId",
                        column: x => x.ArticuloId,
                        principalTable: "Articulos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentoLineas_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    Serie = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NumeroExpedicion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TipoOperacion = table.Column<byte>(type: "INTEGER", nullable: true),
                    CodMoneda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    NIFCliente = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NombreCliente = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
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
                name: "RegistrosVerifactu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NifEmisor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NombreRazonEmisor = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    NumeroFactura = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Serie = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CodMoneda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    FechaExpedicion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipoFactura = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    TipoRectificativa = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    Incidencia = table.Column<bool>(type: "INTEGER", nullable: false),
                    RechazoPrevio = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefExterna = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    FacturasRectificadasJson = table.Column<string>(type: "TEXT", nullable: true),
                    FacturasSustituidasJson = table.Column<string>(type: "TEXT", nullable: true),
                    BaseImponible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CuotaTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    CuotaRecargoEquivalencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    DescripcionOperacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaHoraHusoGeneracion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EsPrimerRegistro = table.Column<bool>(type: "INTEGER", nullable: false),
                    HuellaAnterior = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Huella = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TipoHuella = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    NombreSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VersionSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IdSistemaInformatico = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumeroInstalacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EstadoRemision = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaRemision = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IntentosRemision = table.Column<int>(type: "INTEGER", nullable: false),
                    RespuestaAeat = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoErrorAeat = table.Column<string>(type: "TEXT", nullable: true),
                    DatosRegistroJson = table.Column<string>(type: "TEXT", nullable: false),
                    UrlQr = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FirmaRegistroBase64 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosVerifactu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosVerifactu_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vencimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaPago = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MetodoPago = table.Column<string>(type: "TEXT", nullable: true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vencimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vencimientos_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vencimientos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MovimientosExtracto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExtractoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Secuencia = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaValor = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaContable = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    ContrapartidaNombre = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    ContrapartidaIBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: true),
                    ReferenciaBanco = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    EndToEndId = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    MandateId = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    Conciliado = table.Column<bool>(type: "INTEGER", nullable: false),
                    AsientoContableId = table.Column<int>(type: "INTEGER", nullable: true),
                    OperacionRemesaId = table.Column<int>(type: "INTEGER", nullable: true),
                    FacturaId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaConciliacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosExtracto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosExtracto_ExtractosBancarios_ExtractoId",
                        column: x => x.ExtractoId,
                        principalTable: "ExtractosBancarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemesasSEPA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Esquema = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaBancariaOrdenanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaBancariaAcreedoraId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaEjecucion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    NumeroOperaciones = table.Column<int>(type: "INTEGER", nullable: false),
                    XmlGenerado = table.Column<string>(type: "TEXT", nullable: true),
                    NombreArchivoXml = table.Column<string>(type: "TEXT", nullable: true),
                    HashXmlSHA256 = table.Column<string>(type: "TEXT", nullable: true),
                    TamanoBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    FechaEnvioBanco = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReferenciaBanco = table.Column<string>(type: "TEXT", nullable: true),
                    RespuestaBanco = table.Column<string>(type: "TEXT", nullable: true),
                    Conciliada = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaConciliacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioEnvio = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UsuarioConciliacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MandatoSEPAId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemesasSEPA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemesasSEPA_CuentasBancarias_CuentaBancariaAcreedoraId",
                        column: x => x.CuentaBancariaAcreedoraId,
                        principalTable: "CuentasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemesasSEPA_CuentasBancarias_CuentaBancariaOrdenanteId",
                        column: x => x.CuentaBancariaOrdenanteId,
                        principalTable: "CuentasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemesasSEPA_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RemesasSEPA_MandatosSEPA_MandatoSEPAId",
                        column: x => x.MandatoSEPAId,
                        principalTable: "MandatosSEPA",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DetallesLiquidacionIVA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LiquidacionIVAId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoOperacion = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoIVA = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    TipoDocumento = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroDocumento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FechaOperacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BaseImponible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    TipoImpositivo = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    CuotaIVA = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    SectorDiferenciadoId = table.Column<int>(type: "INTEGER", nullable: true),
                    EsDeducible = table.Column<bool>(type: "INTEGER", nullable: false),
                    PorcentajeDeduccion = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    CuotaDeducible = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    InversionSujetoPasivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegimenEspecial = table.Column<string>(type: "TEXT", nullable: true),
                    AsientoContableId = table.Column<int>(type: "INTEGER", nullable: true),
                    SectorDiferenciadoIVAId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesLiquidacionIVA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesLiquidacionIVA_LiquidacionesIVA_LiquidacionIVAId",
                        column: x => x.LiquidacionIVAId,
                        principalTable: "LiquidacionesIVA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesLiquidacionIVA_SectoresDiferenciadosIVA_SectorDiferenciadoIVAId",
                        column: x => x.SectorDiferenciadoIVAId,
                        principalTable: "SectoresDiferenciadosIVA",
                        principalColumn: "Id");
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
                    DatosRegistroJson = table.Column<string>(type: "TEXT", nullable: false),
                    FechaAnulacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AnulacionPreviaId = table.Column<int>(type: "INTEGER", nullable: true)
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
                name: "Nominas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    Anio = table.Column<int>(type: "INTEGER", nullable: false),
                    SalarioBase = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Complementos = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HorasExtra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PagasExtraProrrateadas = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseContingenciasComunes = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseContingenciasProfesionales = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseHorasExtra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    GrupoCotizacion = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoCuentaCotizacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CuotaSegSocialTrabajador = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TipoIRPF = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 4, nullable: false),
                    RetencionIRPF = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Deducciones = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CuotaSegSocialTrabajadorTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TienePagasExtra = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImportePagasExtra = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    NumeroPagasExtra = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstaPagada = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemesaSEPAId = table.Column<int>(type: "INTEGER", nullable: true),
                    NumeroSeguridadSocial = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    IBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    DiasTrabajados = table.Column<int>(type: "INTEGER", nullable: false),
                    ConceptoPago = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CodPais = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    FechaPago = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaValor = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipoContratoId = table.Column<int>(type: "INTEGER", nullable: true),
                    TipoContrato = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nominas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nominas_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Nominas_RemesasSEPA_RemesaSEPAId",
                        column: x => x.RemesaSEPAId,
                        principalTable: "RemesasSEPA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperacionesRemesaSEPA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RemesaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    BeneficiarioNombre = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    BeneficiarioIBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: true),
                    BeneficiarioBIC = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    BeneficiarioCalle = table.Column<string>(type: "TEXT", maxLength: 70, nullable: true),
                    BeneficiarioNumero = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    BeneficiarioCodigoPostal = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    BeneficiarioPoblacion = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    BeneficiarioPais = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    DeudorNombre = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    DeudorIBAN = table.Column<string>(type: "TEXT", maxLength: 34, nullable: true),
                    DeudorBIC = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    MandatoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReferenciaUnicaMandato = table.Column<string>(type: "TEXT", maxLength: 35, nullable: true),
                    TipoSecuencia = table.Column<int>(type: "INTEGER", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    ReferenciaPropia = table.Column<string>(type: "TEXT", maxLength: 140, nullable: true),
                    OrigenTipo = table.Column<string>(type: "TEXT", nullable: true),
                    OrigenId = table.Column<int>(type: "INTEGER", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoRespuestaBanco = table.Column<string>(type: "TEXT", nullable: true),
                    DescripcionRespuestaBanco = table.Column<string>(type: "TEXT", nullable: true),
                    FechaRespuestaBanco = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EsDevolucion = table.Column<bool>(type: "INTEGER", nullable: false),
                    CodigoDevolucion = table.Column<string>(type: "TEXT", nullable: true),
                    FechaDevolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacionesRemesaSEPA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperacionesRemesaSEPA_MandatosSEPA_MandatoId",
                        column: x => x.MandatoId,
                        principalTable: "MandatosSEPA",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperacionesRemesaSEPA_RemesasSEPA_RemesaId",
                        column: x => x.RemesaId,
                        principalTable: "RemesasSEPA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasTrazabilidad_EmpresaId",
                table: "AlertasTrazabilidad",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasTrazabilidad_LoteId",
                table: "AlertasTrazabilidad",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ApuntesContables_AsientoId",
                table: "ApuntesContables",
                column: "AsientoId");

            migrationBuilder.CreateIndex(
                name: "IX_ApuntesContables_CuentaContableCodigo",
                table: "ApuntesContables",
                column: "CuentaContableCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_FamiliaId",
                table: "Articulos",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_ProveedorHabitualId",
                table: "Articulos",
                column: "ProveedorHabitualId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_EmpresaId",
                table: "AsientosContables",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_LibroDiarioId",
                table: "AsientosContables",
                column: "LibroDiarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDigitales_EmpresaId",
                table: "CertificadosDigitales",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CierresCaja_EmpresaId",
                table: "CierresCaja",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EmpresaId",
                table: "Clientes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunicacionesCertificadas_EmpresaId",
                table: "ComunicacionesCertificadas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesIVA_EjercicioId",
                table: "ConfiguracionesIVA",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesIVA_EmpresaId",
                table: "ConfiguracionesIVA",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlesHorarios_EmpleadoId",
                table: "ControlesHorarios",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasBancarias_EmpresaId",
                table: "CuentasBancarias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_CodigoPadre",
                table: "CuentasContables",
                column: "CodigoPadre");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_EmpresaId",
                table: "CuentasContables",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesLiquidacionIVA_LiquidacionIVAId",
                table: "DetallesLiquidacionIVA",
                column: "LiquidacionIVAId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesLiquidacionIVA_SectorDiferenciadoIVAId",
                table: "DetallesLiquidacionIVA",
                column: "SectorDiferenciadoIVAId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoLineas_ArticuloId",
                table: "DocumentoLineas",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoLineas_DocumentoId",
                table: "DocumentoLineas",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ClienteId",
                table: "Documentos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_EmpresaId_NumeroDocumento",
                table: "Documentos",
                columns: new[] { "EmpresaId", "NumeroDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ProveedorId",
                table: "Documentos",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosContables_EmpresaId",
                table: "EjerciciosContables",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_EmpresaId",
                table: "Empleados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_CertificadoVerifactuId",
                table: "Empresas",
                column: "CertificadoVerifactuId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractosBancarios_CuentaBancariaId",
                table: "ExtractosBancarios",
                column: "CuentaBancariaId");

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
                name: "IX_FirmasElectronicas_CertificadoId",
                table: "FirmasElectronicas",
                column: "CertificadoId");

            migrationBuilder.CreateIndex(
                name: "IX_FirmasElectronicas_EmpresaId",
                table: "FirmasElectronicas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosDiario_EjercicioId",
                table: "LibrosDiario",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosDiario_EmpresaId",
                table: "LibrosDiario",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosInventariosCuentasAnuales_EjercicioId",
                table: "LibrosInventariosCuentasAnuales",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosInventariosCuentasAnuales_EmpresaId",
                table: "LibrosInventariosCuentasAnuales",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosMayor_CuentaContableCodigo",
                table: "LibrosMayor",
                column: "CuentaContableCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosMayor_EjercicioId",
                table: "LibrosMayor",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrosMayor_EmpresaId",
                table: "LibrosMayor",
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
                name: "IX_LiquidacionesIVA_EjercicioId",
                table: "LiquidacionesIVA",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionesIVA_EmpresaId",
                table: "LiquidacionesIVA",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionesSeguridadSocial_EmpresaId",
                table: "LiquidacionesSeguridadSocial",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Llamadas_EmpresaId",
                table: "Llamadas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesTrazabilidad_ArticuloId",
                table: "LotesTrazabilidad",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesTrazabilidad_EmpresaId",
                table: "LotesTrazabilidad",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesTrazabilidad_ProveedorId",
                table: "LotesTrazabilidad",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatosSEPA_CuentaBancariaAcreedorId",
                table: "MandatosSEPA",
                column: "CuentaBancariaAcreedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatosSEPA_CuentaBancariaDeudorId",
                table: "MandatosSEPA",
                column: "CuentaBancariaDeudorId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatosSEPA_EmpresaId",
                table: "MandatosSEPA",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExtracto_ExtractoId",
                table: "MovimientosExtracto",
                column: "ExtractoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosLote_ClienteId",
                table: "MovimientosLote",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosLote_EmpresaId",
                table: "MovimientosLote",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosLote_LoteId",
                table: "MovimientosLote",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosLote_LoteOrigenId",
                table: "MovimientosLote",
                column: "LoteOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosLote_LoteResultadoId",
                table: "MovimientosLote",
                column: "LoteResultadoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosLote_ProveedorId",
                table: "MovimientosLote",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ArticuloId",
                table: "MovimientosStock",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_EmpresaId",
                table: "MovimientosStock",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominas_EmpleadoId",
                table: "Nominas",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominas_RemesaSEPAId",
                table: "Nominas",
                column: "RemesaSEPAId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesRemesaSEPA_MandatoId",
                table: "OperacionesRemesaSEPA",
                column: "MandatoId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesRemesaSEPA_RemesaId",
                table: "OperacionesRemesaSEPA",
                column: "RemesaId");

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
                name: "IX_RegistrosVerifactu_DocumentoId",
                table: "RegistrosVerifactu",
                column: "DocumentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosVerifactu_EmpresaId_FechaHoraHusoGeneracion",
                table: "RegistrosVerifactu",
                columns: new[] { "EmpresaId", "FechaHoraHusoGeneracion" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosVerifactuAnulacion_RegistroAltaId",
                table: "RegistrosVerifactuAnulacion",
                column: "RegistroAltaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemesasSEPA_CuentaBancariaAcreedoraId",
                table: "RemesasSEPA",
                column: "CuentaBancariaAcreedoraId");

            migrationBuilder.CreateIndex(
                name: "IX_RemesasSEPA_CuentaBancariaOrdenanteId",
                table: "RemesasSEPA",
                column: "CuentaBancariaOrdenanteId");

            migrationBuilder.CreateIndex(
                name: "IX_RemesasSEPA_EmpresaId",
                table: "RemesasSEPA",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RemesasSEPA_MandatoSEPAId",
                table: "RemesasSEPA",
                column: "MandatoSEPAId");

            migrationBuilder.CreateIndex(
                name: "IX_RetiradasLote_EmpresaId",
                table: "RetiradasLote",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_RetiradasLote_LoteId",
                table: "RetiradasLote",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SectoresDiferenciadosIVA_EmpresaId",
                table: "SectoresDiferenciadosIVA",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SellosTiempo_EmpresaId",
                table: "SellosTiempo",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFirma_EmpresaId",
                table: "SolicitudesFirma",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFirma_FirmaElectronicaId",
                table: "SolicitudesFirma",
                column: "FirmaElectronicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_EmpresaId",
                table: "Tareas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vencimientos_DocumentoId",
                table: "Vencimientos",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vencimientos_EmpresaId",
                table: "Vencimientos",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertasTrazabilidad_Empresas_EmpresaId",
                table: "AlertasTrazabilidad",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AlertasTrazabilidad_LotesTrazabilidad_LoteId",
                table: "AlertasTrazabilidad",
                column: "LoteId",
                principalTable: "LotesTrazabilidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApuntesContables_AsientosContables_AsientoId",
                table: "ApuntesContables",
                column: "AsientoId",
                principalTable: "AsientosContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApuntesContables_CuentasContables_CuentaContableCodigo",
                table: "ApuntesContables",
                column: "CuentaContableCodigo",
                principalTable: "CuentasContables",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AsientosContables_Empresas_EmpresaId",
                table: "AsientosContables",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AsientosContables_LibrosDiario_LibroDiarioId",
                table: "AsientosContables",
                column: "LibroDiarioId",
                principalTable: "LibrosDiario",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Empresas_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificadosDigitales_Empresas_EmpresaId",
                table: "CertificadosDigitales",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificadosDigitales_Empresas_EmpresaId",
                table: "CertificadosDigitales");

            migrationBuilder.DropTable(
                name: "Acreedores");

            migrationBuilder.DropTable(
                name: "AlertasTrazabilidad");

            migrationBuilder.DropTable(
                name: "ApuntesContables");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CierresCaja");

            migrationBuilder.DropTable(
                name: "ComunicacionesCertificadas");

            migrationBuilder.DropTable(
                name: "ConfiguracionesGenerales");

            migrationBuilder.DropTable(
                name: "ConfiguracionesIVA");

            migrationBuilder.DropTable(
                name: "ControlesHorarios");

            migrationBuilder.DropTable(
                name: "DetallesLiquidacionIVA");

            migrationBuilder.DropTable(
                name: "DocumentoLineas");

            migrationBuilder.DropTable(
                name: "FacturasElectronicas");

            migrationBuilder.DropTable(
                name: "LibrosInventariosCuentasAnuales");

            migrationBuilder.DropTable(
                name: "LibrosMayor");

            migrationBuilder.DropTable(
                name: "LibrosRegistroIVA");

            migrationBuilder.DropTable(
                name: "LiquidacionesSeguridadSocial");

            migrationBuilder.DropTable(
                name: "Llamadas");

            migrationBuilder.DropTable(
                name: "MovimientosExtracto");

            migrationBuilder.DropTable(
                name: "MovimientosLote");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "Nominas");

            migrationBuilder.DropTable(
                name: "OperacionesRemesaSEPA");

            migrationBuilder.DropTable(
                name: "PoliticasControlHorario");

            migrationBuilder.DropTable(
                name: "RegistrosTratamiento");

            migrationBuilder.DropTable(
                name: "RegistrosVerifactuAnulacion");

            migrationBuilder.DropTable(
                name: "RetiradasLote");

            migrationBuilder.DropTable(
                name: "SellosTiempo");

            migrationBuilder.DropTable(
                name: "SolicitudesFirma");

            migrationBuilder.DropTable(
                name: "Tareas");

            migrationBuilder.DropTable(
                name: "TarifasImpuesto");

            migrationBuilder.DropTable(
                name: "Vencimientos");

            migrationBuilder.DropTable(
                name: "AsientosContables");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "LiquidacionesIVA");

            migrationBuilder.DropTable(
                name: "SectoresDiferenciadosIVA");

            migrationBuilder.DropTable(
                name: "CuentasContables");

            migrationBuilder.DropTable(
                name: "ExtractosBancarios");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "RemesasSEPA");

            migrationBuilder.DropTable(
                name: "RegistrosVerifactu");

            migrationBuilder.DropTable(
                name: "LotesTrazabilidad");

            migrationBuilder.DropTable(
                name: "FirmasElectronicas");

            migrationBuilder.DropTable(
                name: "LibrosDiario");

            migrationBuilder.DropTable(
                name: "MandatosSEPA");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "EjerciciosContables");

            migrationBuilder.DropTable(
                name: "CuentasBancarias");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Familia");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "CertificadosDigitales");
        }
    }
}
