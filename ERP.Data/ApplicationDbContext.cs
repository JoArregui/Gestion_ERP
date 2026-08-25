using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities;
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
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Acreedor> Acreedores { get; set; }
        public DbSet<Articulo> Articulos { get; set; }
        public DbSet<Familia> Familias { get; set; } 
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<ControlHorario> ControlesHorarios { get; set; }
        public DbSet<DocumentoComercial> Documentos { get; set; }
        public DbSet<DocumentoLinea> DocumentoLineas { get; set; }
        public DbSet<Vencimiento> Vencimientos { get; set; }
        public DbSet<RegistroVerifactu> RegistrosVerifactu { get; set; }
        public DbSet<Nomina> Nominas { get; set; }
        public DbSet<CierreCaja> CierresCaja { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
        public DbSet<ConfiguracionGeneral> ConfiguracionesGenerales { get; set; }

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
            // Marcamos IsRequired(false) para que EF no se queje si el filtro oculta el artículo
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

            // La referencia comercial debe ser única dentro de cada empresa. Es la
            // última barrera de integridad frente a peticiones concurrentes.
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
        }
    }
}
