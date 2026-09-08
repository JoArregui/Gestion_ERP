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
            // Validación manual para devolver mensaje limpio en lugar de ProblemDetails con "errors.Password"
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Datos de usuario inválidos";
                return BadRequest(new { Message = firstError });
            }
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
                return BadRequest(new { Message = "La política de administración exige un mínimo de 8 caracteres." });

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "El correo electrónico ya está registrado en el sistema" });
            }

            if (!string.IsNullOrEmpty(model.Role))
            {
                var roleExists = await _roleManager.RoleExistsAsync(model.Role);
                if (!roleExists)
                {
                    return BadRequest(new { Message = $"El rol '{model.Role}' no existe en el sistema" });
                }
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                IsActivo = true,
                UltimoAcceso = null, 
                EmpresaId = model.EmpresaId > 0 ? model.EmpresaId : 1
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var addedRoles = new List<string>();
                var addedClaims = new List<System.Security.Claims.Claim>();
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    addedRoles.Add(model.Role);
                }
                // Propagar permisos del rol si existen
                var perms = await _roleManager.GetClaimsAsync(await _roleManager.FindByNameAsync(model.Role ?? "Admin") ?? new IdentityRole());
                foreach (var p in perms.Where(c => c.Type == "Permission"))
                {
                    await _userManager.AddClaimAsync(user, p);
                    addedClaims.Add(p);
                }
                // Duplicar en GestionX.db si tiene EmpresaId (creación desde onboarding privada)
                if (user.EmpresaId.HasValue && user.EmpresaId.Value != 0)
                {
                    try
                    {
                        var emp = await _context.Empresas.FindAsync(user.EmpresaId.Value);
                        if (emp != null)
                        {
                            var tenantPath = _tenantService.GetTenantDbPath(emp.RazonSocial ?? emp.NombreComercial);
                            if (System.IO.File.Exists(tenantPath))
                                await _tenantService.EnsureUserInTenantAsync(user, tenantPath, addedRoles, addedClaims);
                        }
                    }
                    catch { /* master ya tiene usuario, tenant se sincroniza luego */ }
                }

                return Ok(new { Message = "Usuario creado correctamente", UserId = user.Id });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = "Error al crear usuario", Errors = errors });
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