using System.Text.Json;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Fiscal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Services
{
    public static class SeedService
    {
        public static async Task InitializeAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await ctx.Database.EnsureCreatedAsync();

            // 1. Tarifas IVA + IGIC/IPSI 2026
            if (!await ctx.TarifasImpuesto.AnyAsync())
            {
                var tarifas = new List<TarifaImpuesto>
                {
                    // IVA Península/Baleares
                    new() { Territorio = (TerritorioFiscal)0, TipoIVA = (TipoIVA)0, Nombre = "IVA General", Porcentaje = 21m, RecargoEquivalencia = 5.2m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                    new() { Territorio = (TerritorioFiscal)0, TipoIVA = (TipoIVA)1, Nombre = "IVA Reducido", Porcentaje = 10m, RecargoEquivalencia = 1.4m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                    new() { Territorio = (TerritorioFiscal)0, TipoIVA = (TipoIVA)2, Nombre = "IVA Superreducido", Porcentaje = 4m, RecargoEquivalencia = 0.5m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },

                    // IGIC Canarias
                    new() { Territorio = (TerritorioFiscal)1, TipoIVA = (TipoIVA)0, Nombre = "IGIC General", Porcentaje = 7m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                    new() { Territorio = (TerritorioFiscal)1, TipoIVA = (TipoIVA)1, Nombre = "IGIC Reducido", Porcentaje = 3m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                    new() { Territorio = (TerritorioFiscal)1, TipoIVA = (TipoIVA)2, Nombre = "IGIC Superreducido", Porcentaje = 0m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) }, // 0% en textos; es franquicia/modelo 400
                    new() { Territorio = (TerritorioFiscal)1, TipoIVA = (TipoIVA)3, Nombre = "IGIC Incrementado", Porcentaje = 9.5m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                    new() { Territorio = (TerritorioFiscal)1, TipoIVA = (TipoIVA)4, Nombre = "IGIC Especial", Porcentaje = 13.5m, RecargoEquivalencia = 1.75m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                    new() { Territorio = (TerritorioFiscal)1, TipoIVA = (TipoIVA)5, Nombre = "IGIC Petróleo nuevo 2026", Porcentaje = 1m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },

                    // IPSI Ceuta/Melilla (simplificado, 0.5-10%)
                    new() { Territorio = (TerritorioFiscal)2, TipoIVA = (TipoIVA)0, Nombre = "IPSI 0.5%", Porcentaje = 0.5m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                    new() { Territorio = (TerritorioFiscal)2, TipoIVA = (TipoIVA)1, Nombre = "IPSI 10%", Porcentaje = 10m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },

                    // Extranjero UE / No UE (0% exportación intracom)
                    new() { Territorio = (TerritorioFiscal)4, TipoIVA = (TipoIVA)3, Nombre = "IVA Exento 20 LIVA", Porcentaje = 0m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) },
                };
                foreach (var t in tarifas) ctx.TarifasImpuesto.Add(t);
                await ctx.SaveChangesAsync();
            }

            // 2. Política control horario por defecto por empresa
            if (!await ctx.PoliticasControlHorario.AnyAsync())
            {
                // Empresas existentes -> política por defecto
                var empresas = await ctx.Empresas.ToListAsync();
                foreach (var emp in empresas)
                {
                    ctx.PoliticasControlHorario.Add(new PoliticaControlHorario
                    {
                        EmpresaId = emp.Id,
                        RequiereGeolocalizacion = false,
                        PermiteAutoCorreccion = true,
                        MargenToleranciaMinutos = 5,
                        HorasExtraMaxMes = 80,
                        RequiereFirmaCorreccion = true,
                        AniosConservacion = 4,
                        ConvenioReferencia = null,
                        FechaCreacion = DateTime.Now
                    });
                }
                await ctx.SaveChangesAsync();
            }

            // 3. Datos maestros cliente/proveedor (DIR3)
            if (!await ctx.Clientes.AnyAsync(c => c.DIR3_OficinaContable != null || c.DIR3_OrganoGestor != null || c.DIR3_UnidadTramitadora != null || !string.IsNullOrEmpty(c.NIF_UE)))
            {
                var clientes = await ctx.Clientes.ToListAsync();
                foreach (var cli in clientes)
                {
                    cli.DIR3_OficinaContable = cli.DIR3_OficinaContable ?? "";
                    cli.DIR3_OrganoGestor = cli.DIR3_OrganoGestor ?? "";
                    cli.DIR3_UnidadTramitadora = cli.DIR3_UnidadTramitadora ?? "";
                    cli.NIF_UE = cli.NIF_UE ?? "ES";
                    cli.PaisISO = cli.PaisISO ?? "ES";
                    // EsAdministracionPublica es 'bool' (no nullable), no admite '??'.
                    // Se deja tal cual (por defecto ya es false); si se quisiera forzar
                    // explícitamente el valor por defecto, bastaría con:
                    // cli.EsAdministracionPublica = false;
                }
                await ctx.SaveChangesAsync();
            }

            // 4. Config IVA por empresa (puede que ya exista)
            if (!await ctx.ConfiguracionesIVA.AnyAsync())
            {
                var emp = await ctx.Empresas.FirstOrDefaultAsync();
                if (emp != null)
                {
                    ctx.ConfiguracionesIVA.Add(new ConfiguracionIVA
                    {
                        EmpresaId = emp.Id,
                        EjercicioId = 1,
                        AplicaProrrataGeneral = true,
                        PorcentajeProrrataGeneral = 100m,
                        PorcentajeProrrataGeneralRedondeado = 100m,
                        AplicaIVACaja = false,
                        FechaCreacion = DateTime.Now
                    });
                    await ctx.SaveChangesAsync();
                }
            }

            // 5. Importar políticas conocidas a entidades Cliente/Proveedor (DIR3) vacío
            await ctx.SaveChangesAsync();
        }
    }
}