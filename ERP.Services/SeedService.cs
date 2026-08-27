using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Constants; // Importante para acceder a AppPermissions
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Necesario para los permisos
using System.Threading.Tasks;

namespace ERP.Services
{
    public static class SeedService
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. ASEGURAR ROLES DEL SISTEMA
            string[] roles = { "Admin", "Usuario", "Almacen", "Contabilidad" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. ASEGURAR EXISTENCIA DE EMPRESA (Requisito para ApplicationUser)
            var empresaPrincipal = await context.Empresas.FirstOrDefaultAsync();
            if (empresaPrincipal == null)
            {
                empresaPrincipal = new Empresa 
                { 
                    NombreComercial = "SISTEMA ERP GENERICO",
                    CIF = "B00000000",
                    FechaAlta = DateTime.Now,
                    IsActiva = true
                };
                context.Empresas.Add(empresaPrincipal);
                await context.SaveChangesAsync();
            }

            // 3. ASEGURAR USUARIO ADMINISTRADOR
            var adminEmail = "admin@erp.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrador del Sistema",
                    IsActivo = true,
                    EmailConfirmed = true,
                    EmpresaId = empresaPrincipal.Id,
                    UltimoAcceso = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    // Asignar Rol
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // --- ASIGNACIÓN DE PERMISOS (CLAIMS) ---
                    // Esto es lo que hace que el NavMenu se llene de opciones
                    var existingClaims = await userManager.GetClaimsAsync(adminUser);
                    foreach (var permission in AppPermissions.All)
                    {
                        if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                        {
                            await userManager.AddClaimAsync(adminUser, new Claim("Permission", permission));
                        }
                    }
                }
            }

            // 4. CONFIGURACIÓN INICIAL DE PARÁMETROS GENERALES
            if (!await context.ConfiguracionesGenerales.AnyAsync())
            {
                var configs = new List<ConfiguracionGeneral>
                {
                    new ConfiguracionGeneral { Clave = "SMTP_Server", Valor = "smtp.gmail.com", Descripcion = "Servidor de correo outgoing", UltimaModificacion = DateTime.Now },
                    new ConfiguracionGeneral { Clave = "SMTP_Port", Valor = "587", Descripcion = "Puerto SMTP TLS/SSL", UltimaModificacion = DateTime.Now },
                    new ConfiguracionGeneral { Clave = "SMTP_User", Valor = "tu-email@gmail.com", Descripcion = "Usuario para autenticación SMTP", UltimaModificacion = DateTime.Now },
                    new ConfiguracionGeneral { Clave = "SMTP_Pass", Valor = "tu-password", Descripcion = "Contraseña cifrada o de aplicación", UltimaModificacion = DateTime.Now },
                    new ConfiguracionGeneral { Clave = "Empresa_Logo", Valor = "/img/logo.png", Descripcion = "Ruta virtual del logo de la empresa", UltimaModificacion = DateTime.Now }
                };

                await context.ConfiguracionesGenerales.AddRangeAsync(configs);
                await context.SaveChangesAsync();
            }

            // 5. DATOS DE MUESTRA PARA MAESTROS (evita "Sin datos" tras reset de BD el 26/08 por migración Estado)
            // Se crean solo si las tablas están vacías, por lo que no duplica en BD con datos reales.
            if (!await context.Familias.AnyAsync())
            {
                var f1 = new Familia { Nombre = "GENERAL", CodigoInterno = "GEN", Descripcion = "Familia por defecto", IsActiva = true, FechaCreacion = DateTime.Now };
                var f2 = new Familia { Nombre = "ELECTRÓNICA", CodigoInterno = "ELEC", Descripcion = "Material electrónico", IsActiva = true, FechaCreacion = DateTime.Now };
                context.Familias.AddRange(f1, f2);
                await context.SaveChangesAsync();
            }
            if (!await context.Articulos.AnyAsync())
            {
                var famId = await context.Familias.Select(f => f.Id).FirstOrDefaultAsync();
                if (famId == 0) famId = 1;
                var arts = new List<Articulo>
                {
                    new Articulo { Codigo = "ART-001", Descripcion = "Artículo demo 1", FamiliaId = famId, EmpresaId = empresaPrincipal.Id, PrecioCompra = 10, PrecioVenta = 15, Stock = 100, StockMinimo = 10, PorcentajeIva = 21, IsDescatalogado = false },
                    new Articulo { Codigo = "ART-002", Descripcion = "Artículo demo 2", FamiliaId = famId, EmpresaId = empresaPrincipal.Id, PrecioCompra = 20, PrecioVenta = 30, Stock = 50, StockMinimo = 5, PorcentajeIva = 21, IsDescatalogado = false }
                };
                context.Articulos.AddRange(arts);
                await context.SaveChangesAsync();
            }
            if (!await context.Clientes.AnyAsync())
            {
                var cli = new Cliente { CodigoCliente = "CLI-001", RazonSocial = "CLIENTE DEMO S.L.", NombreComercial = "Cliente Demo", CIF = "12345678Z", Direccion = "Calle Ejemplo 1", Poblacion = "Madrid", Provincia = "Madrid", CodigoPostal = "28001", Telefono = "600000001", Email = "demo@cliente.com", EmpresaId = empresaPrincipal.Id, FechaAlta = DateTime.Now, IsActivo = true, FormaPago = "Transferencia", DiaPagoHabitual = 1 };
                context.Clientes.Add(cli);
                await context.SaveChangesAsync();
            }
            if (!await context.Proveedores.AnyAsync())
            {
                var prov = new Proveedor { CIF = "A12345678", RazonSocial = "PROVEEDOR DEMO S.L.", NombreContacto = "Juan Pérez", Email = "proveedor@demo.com", Telefono = "600000002", EsAcreedor = false, IsActivo = true, FechaAlta = DateTime.Now };
                context.Proveedores.Add(prov);
                await context.SaveChangesAsync();
            }
            if (!await context.Acreedores.AnyAsync())
            {
                var acre = new Acreedor { CIF = "B87654321", RazonSocial = "ACREEDOR DEMO S.L.", NombreContacto = "Ana López", Email = "acreedor@demo.com", Telefono = "600000003", EsAcreedor = true, IsActivo = true, FechaAlta = DateTime.Now };
                context.Acreedores.Add(acre);
                await context.SaveChangesAsync();
            }
            if (!await context.Empleados.AnyAsync())
            {
                var emp = new Empleado { DNI = "12345678A", Nombre = "Demo", Apellidos = "Empleado", NumeroSeguridadSocial = "123456789012", PinAcceso = "1234", EmpresaId = empresaPrincipal.Id, Cargo = "Operario", Departamento = "General", Email = "demo@empleado.com", Telefono = "600000004", SalarioBaseMensual = 1500, SalarioBrutoAnual = 18000, FechaAlta = DateTime.Now, VacacionesTotales = 22, VacacionesDisfrutadas = 0 };
                context.Empleados.Add(emp);
                await context.SaveChangesAsync();
            }
        }
    }
}