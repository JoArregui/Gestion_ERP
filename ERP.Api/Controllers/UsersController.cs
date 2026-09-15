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
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ERP.Data.MasterDbContext _context;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ERP.Data.MasterDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        private async Task<List<int>> GetAccessibleEmpresaIds(ApplicationUser user)
        {
            var ids = new List<int>();
            if (user.EmpresaId.HasValue) ids.Add(user.EmpresaId.Value);
            var extras = await _context.UserEmpresas.Where(ue => ue.UserId == user.Id).Select(ue => ue.EmpresaId).ToListAsync();
            ids.AddRange(extras);
            return ids.Distinct().ToList();
        }
        private async Task<bool> SharesEmpresa(ApplicationUser a, ApplicationUser b)
        {
            var aIds = await GetAccessibleEmpresaIds(a);
            var bIds = await GetAccessibleEmpresaIds(b);
            return aIds.Intersect(bIds).Any();
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User);

        // ============================================
        // GESTIÓN DE USUARIOS
        // ============================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            if (IsGeneric) return Ok(new List<UserDto>());
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId!);
            if (currentUser == null) return Unauthorized();
            var myEmpresas = (await GetAccessibleEmpresaIds(currentUser)).ToHashSet();
            if (myEmpresas.Count == 0) return Ok(new List<UserDto>());

            // Carga en lote (4 queries) en vez de N+1 por usuario
            var allUsers = await _userManager.Users.AsNoTracking().ToListAsync();
            var todasUE = await _context.UserEmpresas.AsNoTracking().ToListAsync();
            List<int> Accesibles(ApplicationUser u)
            {
                var ids = new List<int>();
                if (u.EmpresaId.HasValue) ids.Add(u.EmpresaId.Value);
                ids.AddRange(todasUE.Where(ue => ue.UserId == u.Id).Select(ue => ue.EmpresaId));
                return ids.Distinct().ToList();
            }
            // Usuarios que comparten al menos una empresa con el solicitante
            var users = allUsers.Where(u => Accesibles(u).Any(myEmpresas.Contains)).ToList();
            var userList = new List<UserDto>();
            if (users.Count == 0) return Ok(userList);

            var userIds = users.Select(u => u.Id).ToList();
            var rolesByUser = await (from ur in _context.UserRoles.AsNoTracking()
                                     join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                                     where userIds.Contains(ur.UserId)
                                     select new { ur.UserId, r.Name })
                                    .ToListAsync();
            var rolesMap = rolesByUser
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());
            var empresasNecesarias = users.SelectMany(Accesibles).Distinct().ToList();
            var nombresMap = await _context.Empresas.AsNoTracking()
                .Where(e => empresasNecesarias.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.NombreComercial);

            foreach (var user in users)
            {
                var emps = Accesibles(user);
                var role = rolesMap.TryGetValue(user.Id, out var rl) ? rl.FirstOrDefault() ?? "Sin Rol" : "Sin Rol";
                var nombres = emps.Where(nombresMap.ContainsKey).Select(e => nombresMap[e]).ToList();
                userList.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    IsActive = user.IsActivo,
                    Role = role,
                    LastLogin = user.UltimoAcceso,
                    EmpresaId = user.EmpresaId,
                    EmpresaIds = emps,
                    EmpresaNombres = nombres
                });
            }
            return Ok(userList);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetUserById(string id)
        {
            var target = await _userManager.FindByIdAsync(id);
            if (target == null) return NotFound(new { Message = "Usuario no encontrado" });
            if (IsGeneric) return Forbid();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId!);
            if (currentUser == null) return Unauthorized();
            if (!await SharesEmpresa(currentUser, target)) return Forbid();
            var roles = await _userManager.GetRolesAsync(target);
            var emps = await GetAccessibleEmpresaIds(target);
            var nombres = await _context.Empresas.Where(e => emps.Contains(e.Id)).Select(e => e.NombreComercial).ToListAsync();
            return Ok(new UserDto
            {
                Id = target.Id,
                UserName = target.UserName ?? string.Empty,
                Email = target.Email ?? string.Empty,
                FullName = target.FullName,
                IsActive = target.IsActivo,
                Role = roles.FirstOrDefault() ?? "Sin Rol",
                LastLogin = target.UltimoAcceso,
                EmpresaId = target.EmpresaId,
                EmpresaIds = emps,
                EmpresaNombres = nombres
            });
        }

        /// <summary>
        /// Crea un usuario. Los administradores pueden crear usuarios siempre;
        /// el usuario inicial (admin@erp.local, sin rol) SOLO puede crear el PRIMER
        /// usuario de una empresa (Paso 2 del primer onboarding).
        /// </summary>
        [HttpPost]
        [Authorize]
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

            var isAdminCaller = User.IsInRole("Admin");
            var isBootstrapCaller = ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User);
            if (!isAdminCaller && !isBootstrapCaller)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "No tienes permiso para crear usuarios." });
            if (isBootstrapCaller && !isAdminCaller)
            {
                // El usuario inicial exige empresa explícita y que aún no tenga usuarios
                if (model.EmpresaId <= 0)
                    return StatusCode(StatusCodes.Status403Forbidden, new { Message = "El usuario inicial solo puede crear el primer usuario de una empresa (onboarding)." });
                var emp = await _context.Empresas.FindAsync(model.EmpresaId);
                if (emp == null)
                    return BadRequest(new { Message = "Empresa no encontrada para vincular usuario" });
                if (await _userManager.Users.AnyAsync(u => u.EmpresaId == model.EmpresaId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { Message = "El usuario inicial solo puede crear el primer usuario de la empresa." });
            }

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

            var currentUserForCreate = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var myEmpresas = await GetAccessibleEmpresaIds(currentUserForCreate!);
            var requestedIds = (model.EmpresaIds != null && model.EmpresaIds.Any()) ? model.EmpresaIds.Distinct().ToList() : new List<int> { model.EmpresaId };
            requestedIds = requestedIds.Where(id => id > 0).Distinct().ToList();
            if (!requestedIds.Any()) return BadRequest(new { Message = "Debe especificar al menos una empresa válida" });
            if (!IsGeneric)
            {
                if (GetEmpresaId() == 0) return Unauthorized("Sesión sin empresa.");
                if (requestedIds.Except(myEmpresas).Any()) return Forbid();
            }
            else
            {
                var existentes = await _context.Empresas.Where(e => requestedIds.Contains(e.Id) && e.IsActiva).Select(e => e.Id).ToListAsync();
                if (existentes.Count != requestedIds.Count) return BadRequest(new { Message = "Alguna empresa no existe o no está activa" });
            }
            var empresaIdDestino = requestedIds.First();
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                IsActivo = true,
                UltimoAcceso = null,
                EmpresaId = empresaIdDestino
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Guardar multi-empresa adicionales (las que no son la principal)
                foreach (var eid in requestedIds.Skip(1).Distinct())
                {
                    _context.UserEmpresas.Add(new UserEmpresa { UserId = user.Id, EmpresaId = eid });
                }
                // También guardar la principal en la tabla de cruce si quiere consistencia, pero no es necesario: EmpresaId ya la cubre
                // Guardamos todas las adicionales y también la principal como cruce para facilitar queries
                foreach (var eid in requestedIds.Distinct())
                {
                    if (!await _context.UserEmpresas.AnyAsync(ue => ue.UserId == user.Id && ue.EmpresaId == eid))
                        _context.UserEmpresas.Add(new UserEmpresa { UserId = user.Id, EmpresaId = eid });
                }
                await _context.SaveChangesAsync();
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }
                return Ok(new { Message = "Usuario creado correctamente", UserId = user.Id });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = "Error al crear usuario", Errors = errors });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
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
            if (IsGeneric) return Forbid();
            var currentUserForUpd = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!await SharesEmpresa(currentUserForUpd!, user)) return Forbid();
            var requestedUpdIds = (model.EmpresaIds != null && model.EmpresaIds.Any()) ? model.EmpresaIds.Distinct().ToList() : new List<int> { model.EmpresaId };
            requestedUpdIds = requestedUpdIds.Where(x => x > 0).Distinct().ToList();
            if (!requestedUpdIds.Any()) return BadRequest(new { Message = "Debe tener al menos una empresa" });
            var myEmpsUpd = await GetAccessibleEmpresaIds(currentUserForUpd!);
            if (requestedUpdIds.Except(myEmpsUpd).Any()) return Forbid();
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.EmpresaId = requestedUpdIds.First();
            // Sincronizar tabla UserEmpresas
            var existentes = await _context.UserEmpresas.Where(ue => ue.UserId == id).ToListAsync();
            _context.UserEmpresas.RemoveRange(existentes);
            foreach (var eid in requestedUpdIds.Distinct())
                _context.UserEmpresas.Add(new UserEmpresa { UserId = id, EmpresaId = eid });

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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });
            if (IsGeneric) return Forbid();
            var curForStatus = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!await SharesEmpresa(curForStatus!, user)) return Forbid();

            user.IsActivo = !user.IsActivo;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = $"Usuario {(user.IsActivo ? "activado" : "desactivado")} correctamente", IsActive = user.IsActivo });
            }

            return BadRequest(new { Message = "Error al cambiar el estado del usuario" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });
            if (IsGeneric) return Forbid();
            var curForDel = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!await SharesEmpresa(curForDel!, user)) return Forbid();

            if (ERP.Domain.Constants.BootstrapUser.IsBootstrap(user.Email) || ERP.Domain.Constants.BootstrapUser.IsBootstrap(user.UserName))
                return BadRequest(new { Message = "El usuario inicial de onboarding no se puede eliminar." });

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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            if (IsGeneric) return Ok(new List<RoleDto>());
            var empresaId = GetEmpresaId();
            var roles = await _roleManager.Roles.ToListAsync();
            var roleList = new List<RoleDto>();
            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                var filtered = usersInRole.Where(u => u.EmpresaId == empresaId).Count();
                roleList.Add(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name ?? "Sin Nombre",
                    UserCount = filtered
                });
            }
            return Ok(roleList);
        }

        [HttpGet("roles/{roleId}/permissions")]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
