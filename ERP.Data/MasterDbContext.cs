using ERP.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ERP.Data;

/// <summary>
/// Base maestra compartida: identidad, roles, empresas y pertenencias.
/// Los datos operativos se consultan mediante ApplicationDbContext en la base
/// de datos física de la empresa activa.
/// </summary>
public sealed class MasterDbContext : IdentityDbContext<ApplicationUser>
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<Empresa> Empresas { get; set; } = null!;
    public DbSet<UserEmpresa> UserEmpresas { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // La configuración fiscal pertenece a la base operativa. En la base
        // maestra solo se conserva la referencia escalar del certificado.
        modelBuilder.Entity<Empresa>().Ignore(e => e.CertificadoVerifactu);

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
    }
}
