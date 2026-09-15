using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ERP.Services.Tenant.TenantDatabaseService _tenantService;

        public EmpresasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ERP.Services.Tenant.TenantDatabaseService tenantService)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        /// <summary>
        /// Obtiene el listado de empresas visibles para el usuario (pasillo EmpresaId 0 → vacío)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            var empresaId = GetEmpresaId();
            // Pasillo universal (admin@erp.local sin empresa) → programa vacío, sin datos
            if (empresaId == 0) return Ok(new List<Empresa>());
            var empresas = await _context.Empresas
                .AsNoTracking()
                .Where(e => e.IsActiva && e.Id == empresaId)
                .ToListAsync();
            return Ok(empresas);
        }

        /// <summary>
        /// Check onboarding pasillo: ¿hay alguna empresa en el sistema? (sin filtro EmpresaId, para wizard)
        /// </summary>
        [HttpGet("onboarding-check")]
        [AllowAnonymous]
        public async Task<ActionResult> GetOnboardingCheck()
        {
            var count = await _context.Empresas.CountAsync(e => e.IsActiva);
            var hasEmpresa = count > 0;
            var primera = hasEmpresa ? await _context.Empresas.Where(e => e.IsActiva).OrderBy(e => e.Id).Select(e => new { e.Id, e.RazonSocial, e.NombreComercial }).FirstOrDefaultAsync() : null;
            return Ok(new { hasEmpresa, count, pasilloVacio = true, primeraEmpresaId = primera?.Id, primeraRazon = primera?.RazonSocial, primeraNombre = primera?.NombreComercial });
        }

        public class CrearEmpresaOnboardingDto
        {
            public string NombreEmpresa { get; set; } = string.Empty;
            public string? CIF { get; set; }
        }

        /// <summary>
        /// Crea la primera empresa durante el onboarding inicial - CIF real obligatorio, no inventado.
        /// Reservado al usuario inicial (admin@erp.local): es lo único que puede hacer junto al Paso 2.
        /// </summary>
        [Authorize]
        [HttpPost("crear-onboarding")]
        public async Task<ActionResult<Empresa>> CrearParaOnboarding([FromBody] CrearEmpresaOnboardingDto dto)
        {
            if (!ERP.Domain.Constants.BootstrapUser.IsBootstrapUser(User))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Solo el usuario inicial puede crear la empresa del primer onboarding." });

            var nombreEmpresa = dto.NombreEmpresa?.Trim() ?? "";
            var cif = dto.CIF?.Trim().ToUpper() ?? "";
            if (string.IsNullOrWhiteSpace(nombreEmpresa))
                return BadRequest(new { Message = "El nombre de la empresa es obligatorio" });
            if (string.IsNullOrWhiteSpace(cif) || cif.Length < 9)
                return BadRequest(new { Message = "El CIF/NIF real es obligatorio (9 caracteres)" });
            if (!System.Text.RegularExpressions.Regex.IsMatch(cif, @"^[A-Z0-9]{9}$"))
                return BadRequest(new { Message = "CIF/NIF inválido. Formato: A12345678 o B12345678" });

            if (await _context.Empresas.AnyAsync(e => e.CIF == cif && e.IsActiva))
                return BadRequest(new { Message = $"Ya existe una empresa con CIF {cif}" });

            var nombreLimpio = string.Join("-", nombreEmpresa.Split(new[] { ' ', '/', '\\', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => string.Join("", s.Where(char.IsLetterOrDigit))));
            if (string.IsNullOrEmpty(nombreLimpio)) nombreLimpio = "Empresa";

            var empresa = new Empresa
            {
                NombreComercial = nombreLimpio,
                RazonSocial = nombreEmpresa,
                CIF = cif,
                SerieFacturacion = DateTime.UtcNow.Year.ToString(),
                IvaDefecto = 21m,
                IsActiva = true,
                ColorHex = "#3498db",
                Eslogan = null,
                LogoUrl = null,
                LogoBase64 = null,
                FechaAlta = DateTime.UtcNow,
                UltimaModificacion = null,
                TerritorioFiscal = ERP.Domain.Entities.Fiscal.TerritorioFiscal.PeninsulaBaleares,
                EsSII = false
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            // Crear BBDD por empresa: GestionX.db (visible en carpeta, para miles de PCs/empresas)
            // El pasillo admin@erp.local NO se vincula (sigue vacío, sin empresa), el nuevo usuario de Paso 2 se vinculará a esta empresa
            string tenantFile = "";
            try { tenantFile = await _tenantService.EnsureTenantDatabaseAsync(empresa); } catch { }

            return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, new { empresa.Id, empresa.RazonSocial, empresa.NombreComercial, empresa.CIF, TenantDatabase = tenantFile, Mensaje = tenantFile != "" ? $"BBDD {System.IO.Path.GetFileName(tenantFile)} creada" : null });
        }

        /// <summary>
        /// Obtiene el detalle de una empresa por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Empresa>> GetEmpresa(int id)
        {
            // Encapsulación por sesión: cada usuario solo ve su empresa
            if (id != GetEmpresaId()) return Forbid();
            var empresa = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

            if (empresa == null)
            {
                return NotFound(new { Message = "Empresa no encontrada" });
            }

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
                empresa.FechaAlta = DateTime.UtcNow;
                empresa.UltimaModificacion = null;
                
                _context.Empresas.Add(empresa);
                await _context.SaveChangesAsync();

                // Crear BBDD por empresa: GestionX.db (para miles de PCs/empresas)
                // Nota: admin@erp.local (pasillo) NO se vincula nunca, sigue vacío
                try { await _tenantService.EnsureTenantDatabaseAsync(empresa); } catch { }

                return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
            }
            catch
            {
                // Sin Details: no se filtran mensajes técnicos/SQL al cliente.
                return BadRequest(new { Message = "Error al crear la entidad" });
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
            if (id != GetEmpresaId()) return Forbid();

            // Recuperamos la entidad original para no perder la FechaAlta
            var existente = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
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
            if (id != GetEmpresaId()) return Forbid();
            var empresa = await _context.Empresas.FindAsync(id);
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
    }
}