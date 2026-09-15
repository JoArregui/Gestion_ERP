using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Api.Infrastructure;
using ERP.Domain.Entities;
using ERP.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmpresasController : ControllerBase
    {
        private readonly MasterDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TenantDatabaseProvisioner _tenantDatabaseProvisioner;

        public EmpresasController(
            MasterDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TenantDatabaseProvisioner tenantDatabaseProvisioner)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantDatabaseProvisioner = tenantDatabaseProvisioner;
        }

        /// <summary>
        /// Obtiene empresas visibles (RGPD + multi-empresa).
        /// - Genérico bootstrap → solo empresas sin usuarios (recién creadas para onboarding).
        /// - Usuario con N empresas → todas las asignadas.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
            if (user == null) return Unauthorized();
            if (IsGenericBootstrap(user))
            {
                // Las credenciales bootstrap son compartidas y no pueden
                // enumerar empresas huérfanas de otros onboardings.
                return Ok(new List<Empresa>());
            }
            var ids = new List<int>();
            if (user.EmpresaId.HasValue) ids.Add(user.EmpresaId.Value);
            var extras = await _context.UserEmpresas.Where(ue => ue.UserId == user.Id).Select(ue => ue.EmpresaId).ToListAsync();
            ids.AddRange(extras);
            ids = ids.Distinct().ToList();
            if (!ids.Any()) return Ok(new List<Empresa>());
            var list = await _context.Empresas.Where(e => e.IsActiva && ids.Contains(e.Id)).ToListAsync();
            return Ok(list);
        }

        private static bool IsGenericBootstrap(ApplicationUser u)
            => ERP.Domain.Constants.BootstrapUser.IsBootstrap(u.Email)
            || ERP.Domain.Constants.BootstrapUser.IsBootstrap(u.UserName);

        /// <summary>
        /// Crea la primera empresa durante el onboarding inicial
        /// </summary>
        [Authorize]
        [HttpPost("crear-onboarding")]
        public async Task<ActionResult> CrearParaOnboarding([FromBody] BootstrapOnboardingRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
            if (user == null) return Unauthorized();
            if (!IsGenericBootstrap(user)) return Forbid();

            if (string.IsNullOrWhiteSpace(request.NombreEmpresa) ||
                string.IsNullOrWhiteSpace(request.NombreUsuario) ||
                string.IsNullOrWhiteSpace(request.EmailUsuario) ||
                string.IsNullOrWhiteSpace(request.PasswordUsuario))
            {
                return BadRequest(new { Message = "Empresa, nombre, correo y contraseña son obligatorios" });
            }
            if (request.PasswordUsuario.Length < 8)
                return BadRequest(new { Message = "La contraseña debe tener al menos 8 caracteres" });
            if (await _userManager.FindByEmailAsync(request.EmailUsuario.Trim()) != null)
                return Conflict(new { Message = "El correo del usuario ya está registrado" });

            // Limpiar nombre: quitar caracteres especiales, tomar solo letras/números/guiones
            var nombreLimpio = string.Join("-", request.NombreEmpresa.Split(new[] { ' ', '/', '\\', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => string.Join("", s.Where(char.IsLetterOrDigit))));

            // Asegurar que tenga un formato coherente
            if (string.IsNullOrEmpty(nombreLimpio))
                nombreLimpio = "Empresa";

            var tenantProvisioned = false;
            var provisionedEmpresaId = 0;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var empresa = new Empresa
                {
                    NombreComercial = nombreLimpio,
                    RazonSocial = request.NombreEmpresa.Trim(),
                    CIF = $"B{Guid.NewGuid():N}"[..10],
                    SerieFacturacion = DateTime.UtcNow.Year.ToString(),
                    IvaDefecto = 21m,
                    IsActiva = true,
                    ColorHex = "#3498db",
                    FechaAlta = DateTime.UtcNow,
                    TerritorioFiscal = ERP.Domain.Entities.Fiscal.TerritorioFiscal.PeninsulaBaleares
                };
                _context.Empresas.Add(empresa);
                await _context.SaveChangesAsync();
                provisionedEmpresaId = empresa.Id;

                var owner = new ApplicationUser
                {
                    UserName = request.EmailUsuario.Trim(),
                    Email = request.EmailUsuario.Trim(),
                    FullName = request.NombreUsuario.Trim(),
                    EmpresaId = empresa.Id,
                    IsActivo = true,
                    EmailConfirmed = true
                };
                var createUser = await _userManager.CreateAsync(owner, request.PasswordUsuario);
                if (!createUser.Succeeded)
                {
                    var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync();
                    return BadRequest(new { Message = "No se pudo crear el usuario inicial", Errors = errors });
                }

                _context.UserEmpresas.Add(new UserEmpresa { UserId = owner.Id, EmpresaId = empresa.Id });
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                await _userManager.AddToRoleAsync(owner, "Admin");
                await _context.SaveChangesAsync();
                await _tenantDatabaseProvisioner.EnsureCreatedAsync(empresa);
                tenantProvisioned = true;
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, new
                {
                    EmpresaId = empresa.Id,
                    UsuarioCreado = true,
                    Mensaje = "Empresa y usuario creados. Cierra sesión y vuelve a entrar con tus credenciales."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                if (tenantProvisioned)
                    _tenantDatabaseProvisioner.DeleteProvisionedDatabase(provisionedEmpresaId);
                return BadRequest(new { Message = "No se pudo completar el onboarding inicial", Details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene detalle — solo si el usuario pertenece a esa empresa (multi-tenant).
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Empresa>> GetEmpresa(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
            if (user == null) return Unauthorized();
            if (IsGenericBootstrap(user)) return Forbid();
            var ids = new List<int>();
            if (user.EmpresaId.HasValue) ids.Add(user.EmpresaId.Value);
            ids.AddRange(await _context.UserEmpresas.Where(ue => ue.UserId == user.Id).Select(ue => ue.EmpresaId).ToListAsync());
            if (!ids.Contains(id)) return Forbid();
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return NotFound(new { Message = "Empresa no encontrada" });
            return empresa;
        }

        /// <summary>
        /// Registra una nueva sede en el sistema
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Empresa>> PostEmpresa(Empresa empresa)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = currentUserId != null
                    ? await _userManager.FindByIdAsync(currentUserId)
                    : null;
                if (currentUser == null) return Unauthorized();
                if (IsGenericBootstrap(currentUser))
                    return Forbid("El usuario bootstrap debe utilizar el onboarding inicial.");

                // Nunca aceptar la identidad ni relaciones de una entidad enviada
                // por el cliente al crear una empresa.
                empresa.Id = 0;
                empresa.CertificadoVerifactuId = null;
                empresa.CertificadoVerifactu = null;
                empresa.UltimoNumeroFactura = 0;
                empresa.IsActiva = true;
                empresa.FechaAlta = DateTime.UtcNow;
                empresa.UltimaModificacion = null;

                var tenantProvisioned = false;
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Empresas.Add(empresa);
                    await _context.SaveChangesAsync();

                    var userId = currentUser.Id;
                    if (string.IsNullOrEmpty(userId))
                        throw new InvalidOperationException("El usuario autenticado no tiene identificador.");

                    if (currentUser.EmpresaId == null)
                    {
                        currentUser.EmpresaId = empresa.Id;
                        var updateResult = await _userManager.UpdateAsync(currentUser);
                        if (!updateResult.Succeeded)
                            throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));
                    }

                    if (!await _context.UserEmpresas.AnyAsync(ue => ue.UserId == currentUser.Id && ue.EmpresaId == empresa.Id))
                        _context.UserEmpresas.Add(new UserEmpresa { UserId = currentUser.Id, EmpresaId = empresa.Id });
                    await _context.SaveChangesAsync();
                    await _tenantDatabaseProvisioner.EnsureCreatedAsync(empresa);
                    tenantProvisioned = true;
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    if (tenantProvisioned)
                        _tenantDatabaseProvisioner.DeleteProvisionedDatabase(empresa.Id);
                    throw;
                }

                return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error al crear la entidad", Details = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza los datos de una entidad existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpresa(int id, Empresa empresa)
        {
            if (id != empresa.Id)
            {
                return BadRequest(new { Message = "El ID no coincide con la entidad" });
            }

            var existente = await GetEmpresaGestionableAsync(id);
            if (existente == null)
            {
                return NotFound();
            }

            empresa.FechaAlta = existente.FechaAlta;
            empresa.UltimaModificacion = DateTime.UtcNow;

            _context.Entry(empresa).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmpresaExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Desactiva una empresa (Baja lógica) para preservar integridad referencial
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmpresa(int id)
        {
            var empresa = await GetEmpresaGestionableAsync(id);
            if (empresa == null)
            {
                return NotFound();
            }

            // Aplicamos baja lógica
            empresa.IsActiva = false;
            empresa.UltimaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmpresaExists(int id)
        {
            return _context.Empresas.Any(e => e.Id == id);
        }

        private async Task<Empresa?> GetEmpresaGestionableAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
            if (user == null) return null;

            if (IsGenericBootstrap(user))
            {
                // Durante el primer arranque solo se puede gestionar una empresa huérfana.
                var ocupada = await _context.Users.AnyAsync(u => u.EmpresaId == id)
                    || await _context.UserEmpresas.AnyAsync(ue => ue.EmpresaId == id);
                if (ocupada) return null;
            }
            else
            {
                var pertenece = user.EmpresaId == id
                    || await _context.UserEmpresas.AnyAsync(ue => ue.UserId == user.Id && ue.EmpresaId == id);
                if (!pertenece) return null;
            }

            return await _context.Empresas.FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}
