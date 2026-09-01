using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    public static class SeedService
    {
        /// <summary>
        /// Llena datos maestros y configuraciones por defecto una vez la BD está creada/migrada.
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext ctx)
        {
            // 1. Tarifas IVA/IGIC/IPSI 2026 (si tabla vacía)
            if (!await ctx.TarifasImpuesto.AnyAsync())
            {
                var t0 = new TarifaImpuesto { Nombre = "IVA General", Porcentaje = 21m, RecargoEquivalencia = 5.2m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var t1 = new TarifaImpuesto { Nombre = "IVA Reducido", Porcentaje = 10m, RecargoEquivalencia = 1.4m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var t2 = new TarifaImpuesto { Nombre = "IVA Superreducido", Porcentaje = 4m, RecargoEquivalencia = 0.5m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var igic0 = new TarifaImpuesto { Nombre = "IGIC General", Porcentaje = 7m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var igic1 = new TarifaImpuesto { Nombre = "IGIC Reducido", Porcentaje = 3m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var igic2 = new TarifaImpuesto { Nombre = "IGIC Superreducido", Porcentaje = 0m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var igic3 = new TarifaImpuesto { Nombre = "IGIC Incrementado", Porcentaje = 9.5m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var igic4 = new TarifaImpuesto { Nombre = "IGIC Especial", Porcentaje = 13.5m, RecargoEquivalencia = 1.75m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var igic5 = new TarifaImpuesto { Nombre = "IGIC Petróleo nuevo 2026", Porcentaje = 1m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var ipsi0 = new TarifaImpuesto { Nombre = "IPSI 0.5%", Porcentaje = 0.5m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var ipsi1 = new TarifaImpuesto { Nombre = "IPSI 10%", Porcentaje = 10m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };
                var exento = new TarifaImpuesto { Nombre = "IVA Exento 20 LIVA", Porcentaje = 0m, RecargoEquivalencia = 0m, Vigente = true, FechaDesde = new DateTime(2026, 1, 1) };

                // Asignar enum explícitamente después de la creación (evita CS0266 de inicializadores)
                t0.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)0; t0.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)0;
                t1.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)0; t1.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)1;
                t2.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)0; t2.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)2;

                igic0.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)1; igic0.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)0;
                igic1.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)1; igic1.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)1;
                igic2.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)1; igic2.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)2;
                igic3.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)1; igic3.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)3;
                igic4.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)1; igic4.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)4;
                igic5.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)1; igic5.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)5;

                ipsi0.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)2; ipsi0.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)0;
                ipsi1.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)2; ipsi1.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)1;

                exento.Territorio = (ERP.Domain.Entities.Fiscal.TerritorioFiscal)4; exento.TipoIVA = (ERP.Domain.Entities.Fiscal.TipoIVA)5;

                ctx.TarifasImpuesto.AddRange(t0, t1, t2, igic0, igic1, igic2, igic3, igic4, igic5, ipsi0, ipsi1, exento);
                await ctx.SaveChangesAsync();
            }

            // 2. Política control horario por defecto por empresa existente
            if (!await ctx.PoliticasControlHorario.AnyAsync())
            {
                var emp = await ctx.Empresas.FirstOrDefaultAsync();
                if (emp != null)
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
                    await ctx.SaveChangesAsync();
                }
            }

            // 3. Config IVA por empresa si no existe
            if (!await ctx.ConfiguracionesIVA.AnyAsync())
            {
                var e = await ctx.Empresas.FirstOrDefaultAsync();
                if (e != null)
                {
                    ctx.ConfiguracionesIVA.Add(new ConfiguracionIVA
                    {
                        EmpresaId = e.Id,
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

            // 4. Datos maestros cliente/proveedor: DIR3 y UE
            // Cliente sí tiene DIR3_* y NIF_UE; Proveedor solo tiene NIF_UE, PaisISO, EsAdministracionPublica
            if (!await ctx.Clientes.AnyAsync(c => c.DIR3_OficinaContable != null || c.DIR3_OrganoGestor != null || c.DIR3_UnidadTramitadora != null || c.NIF_UE != null || c.NIF_UE != "" || c.EsAdministracionPublica == true))
            {
                var clientes = await ctx.Clientes.ToListAsync();
                foreach (var c in clientes)
                {
                    c.DIR3_OficinaContable = "";
                    c.DIR3_OrganoGestor = "";
                    c.DIR3_UnidadTramitadora = "";
                    c.NIF_UE = "ES";
                    c.PaisISO = "ES";
                    c.EsAdministracionPublica = false;
                }
                await ctx.SaveChangesAsync();
            }

            // Proveedor: solo propiedades que realmente tiene (NIF_UE, PaisISO, EsAdministracionPublica)
            if (!await ctx.Proveedores.AnyAsync(p => p.NIF_UE != null || p.NIF_UE != "" || p.EsAdministracionPublica == true))
            {
                var provs = await ctx.Proveedores.ToListAsync();
                foreach (var p in provs)
                {
                    p.NIF_UE = "ES";
                    p.PaisISO = "ES";
                    p.EsAdministracionPublica = false;
                }
                await ctx.SaveChangesAsync();
            }

            // 5. Certificado Verifactu por defecto: dejaremos al admin, aquí solo aseguramos nullable.

            await Task.CompletedTask;
        }
    }
}