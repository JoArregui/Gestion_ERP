using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Contabilidad;
using ERP.Domain.Entities.Bancario;
using ERP.Domain.Entities.Trazabilidad;
using ERP.Domain.Entities.Fiscal;
using ERP.Domain.Entities.FirmaDigital;
using ERP.Domain.Entities.RGPD;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Linq;

namespace ERP.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<UserEmpresa> UserEmpresas { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Acreedor> Acreedores { get; set; }
        public DbSet<Articulo> Articulos { get; set; }
        public DbSet<Familia> Familia { get; set; } 
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<ControlHorario> ControlesHorarios { get; set; }
        public DbSet<PoliticaControlHorario> PoliticasControlHorario { get; set; }
        public DbSet<DocumentoComercial> Documentos { get; set; }
        public DbSet<DocumentoLinea> DocumentoLineas { get; set; }
        public DbSet<Vencimiento> Vencimientos { get; set; }
        public DbSet<RegistroVerifactu> RegistrosVerifactu { get; set; }
        public DbSet<RegistroVerifactuAnulacion> RegistrosVerifactuAnulacion { get; set; }
        public DbSet<FacturaElectronica> FacturasElectronicas { get; set; }
        public DbSet<Nomina> Nominas { get; set; }
        public DbSet<CierreCaja> CierresCaja { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
        public DbSet<ConfiguracionGeneral> ConfiguracionesGenerales { get; set; }
        
        // Entidades para Tareas y Llamadas
        public DbSet<Tarea> Tareas { get; set; }
        public DbSet<Llamada> Llamadas { get; set; }

        // Entidades Contabilidad
        public DbSet<CuentaContable> CuentasContables { get; set; }
        public DbSet<AsientoContable> AsientosContables { get; set; }
        public DbSet<ApunteContable> ApuntesContables { get; set; }
        public DbSet<LibroDiario> LibrosDiario { get; set; }
        public DbSet<LibroMayor> LibrosMayor { get; set; }
        public DbSet<LibroInventariosCuentasAnuales> LibrosInventariosCuentasAnuales { get; set; }
        public DbSet<EjercicioContable> EjerciciosContables { get; set; }
        // Entidades Bancario
        public DbSet<CuentaBancaria> CuentasBancarias { get; set; }
        public DbSet<MandatoSEPA> MandatosSEPA { get; set; }
        public DbSet<RemesaSEPA> RemesasSEPA { get; set; }
        public DbSet<OperacionRemesaSEPA> OperacionesRemesaSEPA { get; set; }
        public DbSet<ExtractoBancario> ExtractosBancarios { get; set; }
        public DbSet<MovimientoExtracto> MovimientosExtracto { get; set; }
        // Entidades Trazabilidad
        public DbSet<LoteTrazabilidad> LotesTrazabilidad { get; set; }
        public DbSet<MovimientoLote> MovimientosLote { get; set; }
        public DbSet<AlertaTrazabilidad> AlertasTrazabilidad { get; set; }
        public DbSet<RetiradaLote> RetiradasLote { get; set; }
        // Entidades Fiscal
        public DbSet<ConfiguracionIVA> ConfiguracionesIVA { get; set; }
        public DbSet<LiquidacionIVA> LiquidacionesIVA { get; set; }
        public DbSet<DetalleLiquidacionIVA> DetallesLiquidacionIVA { get; set; }
        public DbSet<SectorDiferenciadoIVA> SectoresDiferenciadosIVA { get; set; }
        public DbSet<TarifaImpuesto> TarifasImpuesto { get; set; }
        public DbSet<LibroRegistroIVA> LibrosRegistroIVA { get; set; }
        // Entidades FirmaDigital
        public DbSet<CertificadoDigital> CertificadosDigitales { get; set; }
        public DbSet<FirmaElectronica> FirmasElectronicas { get; set; }
        public DbSet<SolicitudFirma> SolicitudesFirma { get; set; }
        public DbSet<SelloTiempo> SellosTiempo { get; set; }
        public DbSet<ComunicacionCertificada> ComunicacionesCertificadas { get; set; }
        // RGPD + Seguridad Social
        public DbSet<RegistroTratamiento> RegistrosTratamiento { get; set; }
        public DbSet<LiquidacionSeguridadSocial> LiquidacionesSeguridadSocial { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. FILTROS GLOBALES (Tu lógica de negocio) ---
            modelBuilder.Entity<Cliente>().HasQueryFilter(c => c.IsActivo);
            modelBuilder.Entity<Articulo>().HasQueryFilter(a => !a.IsDescatalogado);
            modelBuilder.Entity<Empleado>().HasQueryFilter(e => e.FechaBaja == null || e.FechaBaja > DateTime.Now);
            modelBuilder.Entity<Proveedor>().HasQueryFilter(p => p.IsActivo);
            modelBuilder.Entity<Acreedor>().HasQueryFilter(a => a.IsActivo);
            modelBuilder.Entity<Familia>().HasQueryFilter(f => f.IsActiva);
            modelBuilder.Entity<Familia>().ToTable("Familia"); 

            // --- 2. CONFIGURACIÓN DE PRECISIÓN DECIMAL ---
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

                foreach (var property in properties)
                {
                    property.SetPrecision(18);
                    property.SetScale(4);
                }
            }

            // --- 3. RELACIONES (Solución a Warnings de EF Core) ---
            
            // Relación Articulo -> DocumentoLinea
            modelBuilder.Entity<DocumentoLinea>()
                .HasOne(l => l.Articulo)
                .WithMany()
                .HasForeignKey(l => l.ArticuloId)
                .IsRequired(false) 
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Empleado -> ControlHorario
            modelBuilder.Entity<ControlHorario>()
                .HasOne(c => c.Empleado)
                .WithMany(e => e.ControlesHorarios)
                .HasForeignKey(c => c.EmpleadoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Empleado -> Nomina
            modelBuilder.Entity<Nomina>()
                .HasOne(n => n.Empleado)
                .WithMany(e => e.Nominas)
                .HasForeignKey(n => n.EmpleadoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Articulo -> MovimientoStock
            modelBuilder.Entity<MovimientoStock>()
                .HasOne(m => m.Articulo)
                .WithMany()
                .HasForeignKey(m => m.ArticuloId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // --- RESTO DE CONFIGURACIONES ESTÁNDAR ---

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Empresa)
                .WithMany()
                .HasForeignKey(u => u.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserEmpresa>()
                .HasKey(ue => new { ue.UserId, ue.EmpresaId });
            modelBuilder.Entity<UserEmpresa>()
                .HasOne(ue => ue.User)
                .WithMany(u => u.UserEmpresas)
                .HasForeignKey(ue => ue.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserEmpresa>()
                .HasOne(ue => ue.Empresa)
                .WithMany()
                .HasForeignKey(ue => ue.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Multi-tenant: Familia / Proveedor / Acreedor aislados por Empresa
            modelBuilder.Entity<Familia>()
                .HasOne(f => f.Empresa)
                .WithMany()
                .HasForeignKey(f => f.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Proveedor>()
                .HasOne(p => p.Empresa)
                .WithMany()
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Acreedor>()
                .HasOne(a => a.Empresa)
                .WithMany()
                .HasForeignKey(a => a.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Articulo>()
                .HasOne(a => a.Familia)
                .WithMany(f => f.Articulos)
                .HasForeignKey(a => a.FamiliaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vencimiento>()
                .HasOne(v => v.Empresa)
                .WithMany()
                .HasForeignKey(v => v.EmpresaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DocumentoComercial>()
                .HasOne(d => d.Cliente)
                .WithMany()
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentoComercial>()
                .HasOne(d => d.Proveedor)
                .WithMany()
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentoComercial>()
                .HasIndex(d => new { d.EmpresaId, d.NumeroDocumento })
                .IsUnique();

            modelBuilder.Entity<RegistroVerifactu>()
                .HasIndex(r => r.DocumentoId)
                .IsUnique();

            modelBuilder.Entity<RegistroVerifactu>()
                .HasIndex(r => new { r.EmpresaId, r.FechaHoraHusoGeneracion });
            
            modelBuilder.Entity<DocumentoLinea>()
                .HasOne(l => l.Documento)
                .WithMany(d => d.Lineas)
                .HasForeignKey(l => l.DocumentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CierreCaja>()
                .HasOne(c => c.Empresa)
                .WithMany()
                .HasForeignKey(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración relaciones CuentaBancaria - MandatoSEPA
            modelBuilder.Entity<MandatoSEPA>()
                .HasOne(m => m.CuentaBancariaAcreedor)
                .WithMany(c => c.MandatosAcreedor)
                .HasForeignKey(m => m.CuentaBancariaAcreedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MandatoSEPA>()
                .HasOne(m => m.CuentaBancariaDeudor)
                .WithMany(c => c.MandatosDeudor)
                .HasForeignKey(m => m.CuentaBancariaDeudorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración relaciones RemesaSEPA - CuentaBancaria
            modelBuilder.Entity<RemesaSEPA>()
                .HasOne(r => r.CuentaBancariaOrdenante)
                .WithMany(c => c.RemesasOrdenante)
                .HasForeignKey(r => r.CuentaBancariaOrdenanteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RemesaSEPA>()
                .HasOne(r => r.CuentaBancariaAcreedora)
                .WithMany(c => c.RemesasBeneficiaria)
                .HasForeignKey(r => r.CuentaBancariaAcreedoraId)
                .OnDelete(DeleteBehavior.Restrict);
            // Configuración relaciones Trazabilidad
            modelBuilder.Entity<MovimientoLote>()
                .HasOne(m => m.Lote)
                .WithMany(l => l.MovimientosEntrada)
                .HasForeignKey(m => m.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoLote>()
                .HasOne(m => m.LoteOrigen)
                .WithMany(l => l.MovimientosSalida)
                .HasForeignKey(m => m.LoteOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoLote>()
                .HasOne(m => m.LoteResultado)
                .WithMany()
                .HasForeignKey(m => m.LoteResultadoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlertaTrazabilidad>()
                .HasOne(a => a.Lote)
                .WithMany(l => l.Alertas)
                .HasForeignKey(a => a.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RetiradaLote>()
                .HasOne(r => r.Lote)
                .WithMany(l => l.Retiradas)
                .HasForeignKey(r => r.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            // PoliticaControlHorario -> Empresa
            modelBuilder.Entity<PoliticaControlHorario>()
                .HasOne(p => p.Empresa)
                .WithMany()
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PoliticaControlHorario>()
                .HasIndex(p => p.EmpresaId)
                .IsUnique();

            // RegistroVerifactuAnulacion
            modelBuilder.Entity<RegistroVerifactuAnulacion>()
                .HasOne(r => r.RegistroAlta)
                .WithMany()
                .HasForeignKey(r => r.RegistroAltaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RegistroVerifactuAnulacion>()
                .HasIndex(r => r.RegistroAltaId)
                .IsUnique();

            // FacturaElectronica
            modelBuilder.Entity<FacturaElectronica>()
                .HasOne(f => f.Documento)
                .WithMany()
                .HasForeignKey(f => f.DocumentoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FacturaElectronica>()
                .HasIndex(f => f.DocumentoId)
                .IsUnique();
            modelBuilder.Entity<FacturaElectronica>()
                .HasOne(f => f.Empresa)
                .WithMany()
                .HasForeignKey(f => f.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Nomina -> RemesaSEPA
            modelBuilder.Entity<Nomina>()
                .HasOne(n => n.RemesaSEPA)
                .WithMany()
                .HasForeignKey(n => n.RemesaSEPAId)
                .OnDelete(DeleteBehavior.Restrict);

            // Empresa -> CertificadoVerifactu
            modelBuilder.Entity<Empresa>()
                .HasOne(e => e.CertificadoVerifactu)
                .WithMany()
                .HasForeignKey(e => e.CertificadoVerifactuId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}