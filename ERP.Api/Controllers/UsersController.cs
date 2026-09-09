using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities;
using ERP.Domain.Dtos;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ERP.Data.ApplicationDbContext _context;
        private readonly ERP.Services.Tenant.TenantDatabaseService _tenantService;
        private readonly IConfiguration _config;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ERP.Data.ApplicationDbContext context, ERP.Services.Tenant.TenantDatabaseService tenantService, IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _tenantService = tenantService;
            _config = config;
        }

        // ============================================
        // GESTIÓN DE USUARIOS
        // ============================================

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var empresaId = GetEmpresaId();
            var users = empresaId == 0
                ? await _userManager.Users.Where(u => false).ToListAsync() // pasillo → vacío
                : await _userManager.Users.Where(u => u.EmpresaId == empresaId).ToListAsync();
            var userList = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    IsActive = user.IsActivo,
                    Role = roles.FirstOrDefault() ?? "Sin Rol",
                    LastLogin = user.UltimoAcceso
                });
            }

            return Ok(userList);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsActive = user.IsActivo,
                Role = roles.FirstOrDefault() ?? "Sin Rol",
                LastLogin = user.UltimoAcceso
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Datos de usuario inválidos";
                return BadRequest(new { Message = firstError });
            }
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
                return BadRequest(new { Message = "La política de administración exige un mínimo de 8 caracteres." });

            // Flujo exclusivo: usuario privado se crea SOLO en GestionX.db, no en BBDD INICIAL
            // La BBDD INICIAL solo guarda admin@erp.local (pasillo virgen)
            if (model.EmpresaId > 0)
            {
                var emp = await _context.Empresas.FindAsync(model.EmpresaId);
                if (emp == null) return BadRequest(new { Message = "Empresa no encontrada para vincular usuario" });
                var tenantPath = _tenantService.GetTenantDbPath(emp.RazonSocial ?? emp.NombreComercial);
                // Asegurar DB tenant existe (onboarding ya la creó)
                if (!System.IO.File.Exists(tenantPath))
                {
                    try { await _tenantService.EnsureTenantDatabaseAsync(emp); } catch { }
                }
                // Crear UserManager aislado para este tenant
                var tOpts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ERP.Data.ApplicationDbContext>();
                tOpts.UseSqlite($"Data Source={tenantPath}");
                using var tCtx = new ERP.Data.ApplicationDbContext(tOpts.Options);
                // Asegurar roles en tenant
                var tStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(tCtx);
                var tHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
                using var tUserManager = new UserManager<ApplicationUser>(tStore, null, tHasher, null, null, null, null, null, null);
                var tRoleStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<IdentityRole>(tCtx);
                using var tRoleManager = new RoleManager<IdentityRole>(tRoleStore, null, null, null, null);
                if (!string.IsNullOrEmpty(model.Role) && !await tRoleManager.RoleExistsAsync(model.Role))
                    await tRoleManager.CreateAsync(new IdentityRole(model.Role));

                var existingInTenant = await tUserManager.FindByEmailAsync(model.Email);
                if (existingInTenant != null) return BadRequest(new { Message = "El correo electrónico ya está registrado en la empresa" });

                var tUser = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsActivo = true,
                    UltimoAcceso = null,
                    EmpresaId = model.EmpresaId
                };
                var tResult = await tUserManager.CreateAsync(tUser, model.Password);
                if (!tResult.Succeeded)
                {
                    var errs = string.Join(", ", tResult.Errors.Select(e => e.Description));
                    return BadRequest(new { Message = "Error al crear usuario en empresa", Errors = errs });
                }
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await tUserManager.AddToRoleAsync(tUser, model.Role);
                    var perms = await tRoleManager.GetClaimsAsync(await tRoleManager.FindByNameAsync(model.Role) ?? new IdentityRole());
                    foreach (var p in perms.Where(c => c.Type == "Permission"))
                        await tUserManager.AddClaimAsync(tUser, p);
                }
                // También propagar permisos del rol si el maestro los tiene (fallback)
                if (!string.IsNullOrEmpty(model.Role))
                {
                    var masterRole = await _roleManager.FindByNameAsync(model.Role);
                    if (masterRole != null)
                    {
                        var masterPerms = await _roleManager.GetClaimsAsync(masterRole);
                        foreach (var p in masterPerms.Where(c => c.Type == "Permission"))
                        {
                            var already = (await tUserManager.GetClaimsAsync(tUser)).Any(c => c.Type == p.Type && c.Value == p.Value);
                            if (!already) await tUserManager.AddClaimAsync(tUser, p);
                        }
                    }
                }
                return Ok(new { Message = "Usuario creado correctamente en empresa", UserId = tUser.Id });
            }
            else
            {
                // Sin empresa -> pasillo (no debería usarse, solo admin inicial)
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null) return BadRequest(new { Message = "El correo electrónico ya está registrado en el sistema" });
                if (!string.IsNullOrEmpty(model.Role))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(model.Role);
                    if (!roleExists) return BadRequest(new { Message = $"El rol '{model.Role}' no existe en el sistema" });
                }
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsActivo = true,
                    UltimoAcceso = null,
                    EmpresaId = null
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role)) await _userManager.AddToRoleAsync(user, model.Role);
                    return Ok(new { Message = "Usuario creado correctamente", UserId = user.Id });
                }
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { Message = "Error al crear usuario", Errors = errors });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] CreateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Datos inválidos";
                return BadRequest(new { Message = firstError });
            }
            if (!string.IsNullOrEmpty(model.Password) && model.Password.Length < 8)
                return BadRequest(new { Message = "La política de administración exige un mínimo de 8 caracteres." });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.EmpresaId = model.EmpresaId; 

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passResult.Succeeded)
                    {
                        var passErrors = string.Join(", ", passResult.Errors.Select(e => e.Description));
                        return BadRequest(new { Message = "Usuario actualizado pero la contraseña no pudo cambiarse", Errors = passErrors });
                    }
                }

                return Ok(new { Message = "Usuario actualizado correctamente" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = "Error al actualizar usuario", Errors = errors });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });

            user.IsActivo = !user.IsActivo;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = $"Usuario {(user.IsActivo ? "activado" : "desactivado")} correctamente", IsActive = user.IsActivo });
            }

            return BadRequest(new { Message = "Error al cambiar el estado del usuario" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return BadRequest(new { Message = "No se puede eliminar un usuario con rol de Administrador" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { Message = "Usuario eliminado correctamente" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = "Error al eliminar usuario", Errors = errors });
        }

        // ============================================
        // GESTIÓN DE ROLES Y PERMISOS (RBAC)
        // ============================================

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var empresaId = GetEmpresaId();
            var roles = await _roleManager.Roles.ToListAsync();
            var roleList = new List<RoleDto>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                var count = empresaId == 0 ? 0 : usersInRole.Count(u => u.EmpresaId == empresaId);
                roleList.Add(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name ?? "Sin Nombre",
                    UserCount = count
                });
            }

            return Ok(roleList);
        }

        [HttpGet("roles/{roleId}/permissions")]
        public async Task<ActionResult<RolePermissionDto>> GetRolePermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound(new { Message = "Rol no encontrado" });

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            
            // Definición maestra de permisos del ERP
            var allPermissions = new List<PermissionItemDto>
            {
                new PermissionItemDto { Group = "Seguridad", Value = "Seguridad.Usuarios", Description = "Gestionar usuarios y accesos" },
                new PermissionItemDto { Group = "Seguridad", Value = "Seguridad.Roles", Description = "Configurar matriz de permisos" },
                new PermissionItemDto { Group = "Producción", Value = "Prod.Ver", Description = "Visualizar órdenes de producción" },
                new PermissionItemDto { Group = "Producción", Value = "Prod.Editar", Description = "Crear y modificar procesos" },
                new PermissionItemDto { Group = "Inventario", Value = "Inv.Stock", Description = "Ajustar niveles de stock" },
                new PermissionItemDto { Group = "Ventas", Value = "Ventas.Facturar", Description = "Emitir comprobantes fiscales" }
            };

            foreach (var p in allPermissions)
            {
                p.IsSelected = existingClaims.Any(c => c.Type == "Permission" && c.Value == p.Value);
            }

            return Ok(new RolePermissionDto 
            { 
                RoleId = role.Id, 
                RoleName = role.Name!, 
                Permissions = allPermissions 
            });
        }

        [HttpPost("roles/permissions")]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] RolePermissionDto model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound(new { Message = "Rol no encontrado" });

            var currentClaims = await _roleManager.GetClaimsAsync(role);
            
            // Limpiamos los permisos actuales
            foreach (var claim in currentClaims.Where(c => c.Type == "Permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            // Agregamos los nuevos seleccionados
            foreach (var permission in model.Permissions.Where(x => x.IsSelected))
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", permission.Value));
            }

            return Ok(new { Message = $"Permisos para el rol '{role.Name}' actualizados correctamente" });
        }
    }
}