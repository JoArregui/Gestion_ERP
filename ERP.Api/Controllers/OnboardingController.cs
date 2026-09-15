using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Dtos;
using ERP.Domain.Entities;
using System.Security.Claims;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly MasterDbContext _masterContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public OnboardingController(
            ApplicationDbContext context,
            MasterDbContext masterContext,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _masterContext = masterContext;
            _userManager = userManager;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        /// <summary>
        /// Estado del 2º onboarding (tutorial) para el usuario actual.
        /// Se muestra cuando: tiene empresa asignada, no es bootstrap, no ha sido visto/completado,
        /// y faltan maestros obligatorios.
        /// </summary>
        [HttpGet("setup-status")]
        public async Task<ActionResult<SetupStatusDto>> GetSetupStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var empresaId = GetEmpresaId();
            if (empresaId == 0)
                empresaId = user.EmpresaId ?? 0;
            if (empresaId == 0)
            {
                return Ok(new SetupStatusDto
                {
                    EmpresaId = 0,
                    UserId = userId,
                    Dismissed = true,
                    Completado = true,
                    DebeMostrar = false
                });
            }

            var isBootstrap = ERP.Domain.Constants.BootstrapUser.IsBootstrap(user.Email)
                           || ERP.Domain.Constants.BootstrapUser.IsBootstrap(user.UserName);

            int familias = 0, articulos = 0, proveedores = 0, clientes = 0, empleados = 0;
            if (!isBootstrap)
            {
                familias = await _context.Familia.IgnoreQueryFilters().CountAsync(f => f.EmpresaId == empresaId && f.IsActiva);
                try { articulos = await _context.Articulos.IgnoreQueryFilters().CountAsync(a => a.EmpresaId == empresaId); } catch { articulos = await _context.Articulos.CountAsync(a => a.EmpresaId == empresaId); }
                proveedores = await _context.Proveedores.IgnoreQueryFilters().CountAsync(p => p.EmpresaId == empresaId && p.IsActivo);
                try { clientes = await _context.Clientes.IgnoreQueryFilters().CountAsync(c => c.EmpresaId == empresaId); } catch { clientes = await _context.Clientes.CountAsync(c => c.EmpresaId == empresaId); }
                try { empleados = await _context.Empleados.IgnoreQueryFilters().CountAsync(e => e.EmpresaId == empresaId); } catch { empleados = await _context.Empleados.CountAsync(e => e.EmpresaId == empresaId); }
            }

            var obligatoriosCompletados = 0;
            if (familias > 0) obligatoriosCompletados++;
            if (articulos > 0) obligatoriosCompletados++;

            var membership = await _masterContext.UserEmpresas
                .FirstOrDefaultAsync(x => x.UserId == userId && x.EmpresaId == empresaId);
            var setupVisto = membership?.SetupTutorialVisto ?? user.SetupTutorialVisto;
            var setupCompletado = membership?.SetupTutorialCompletado ?? user.SetupTutorialCompletado;

            var debeMostrar = !isBootstrap
                && !setupVisto
                && !setupCompletado
                && empresaId != 0;

            return Ok(new SetupStatusDto
            {
                EmpresaId = empresaId,
                UserId = userId,
                Familias = familias,
                Articulos = articulos,
                Proveedores = proveedores,
                Clientes = clientes,
                Empleados = empleados,
                Dismissed = setupVisto,
                Completado = setupCompletado,
                DebeMostrar = debeMostrar,
                PasosObligatoriosCompletados = obligatoriosCompletados,
                PasosObligatoriosTotales = 2
            });
        }

        [HttpPost("setup-dismiss")]
        public async Task<ActionResult<SetupActionResultDto>> Dismiss()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return NotFound();
            var empresaId = GetEmpresaId();
            var membership = await GetMembershipAsync(user.Id, empresaId);
            if (membership != null)
            {
                membership.SetupTutorialVisto = true;
                await _masterContext.SaveChangesAsync();
            }
            else
            {
                user.SetupTutorialVisto = true;
                await _userManager.UpdateAsync(user);
            }
            return Ok(new SetupActionResultDto { Ok = true, Message = "Tutorial descartado. No volverá a mostrarse." });
        }

        [HttpPost("setup-complete")]
        public async Task<ActionResult<SetupActionResultDto>> Complete()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return NotFound();
            var empresaId = GetEmpresaId();
            var membership = await GetMembershipAsync(user.Id, empresaId);
            if (membership != null)
            {
                membership.SetupTutorialVisto = true;
                membership.SetupTutorialCompletado = true;
                await _masterContext.SaveChangesAsync();
            }
            else
            {
                user.SetupTutorialVisto = true;
                user.SetupTutorialCompletado = true;
                await _userManager.UpdateAsync(user);
            }
            return Ok(new SetupActionResultDto { Ok = true, Message = "¡Tutorial completado!" });
        }

        [HttpPost("setup-reset")]
        public async Task<ActionResult<SetupActionResultDto>> Reset(string? userId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var targetId = userId ?? currentUserId;
            if (targetId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Forbid();
            var membership = await GetMembershipAsync(targetId!, empresaId);
            if (membership == null) return Forbid();
            membership.SetupTutorialVisto = false;
            membership.SetupTutorialCompletado = false;
            await _masterContext.SaveChangesAsync();
            var user = await _userManager.FindByIdAsync(targetId!);
            if (user == null) return NotFound();
            return Ok(new SetupActionResultDto { Ok = true, Message = "Tutorial reiniciado." });
        }

        private Task<UserEmpresa?> GetMembershipAsync(string userId, int empresaId)
            => _masterContext.UserEmpresas
                .FirstOrDefaultAsync(x => x.UserId == userId && x.EmpresaId == empresaId);
    }
}
