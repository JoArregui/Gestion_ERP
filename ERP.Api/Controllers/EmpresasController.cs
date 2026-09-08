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

        /// <summary>
        /// Obtiene el listado completo de empresas activas
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            var empresas = await _context.Empresas
                .Where(e => e.IsActiva)
                .ToListAsync();

            return Ok(empresas);
        }

        /// <summary>
        /// Crea la primera empresa durante el onboarding inicial
        /// </summary>
        [AllowAnonymous]
        [HttpPost("crear-onboarding")]
        public async Task<ActionResult<Empresa>> CrearParaOnboarding([FromBody] string nombreEmpresa)
        {
            if (string.IsNullOrWhiteSpace(nombreEmpresa))
            {
                return BadRequest(new { Message = "El nombre de la empresa es obligatorio" });
            }

            // Limpiar nombre: quitar caracteres especiales, tomar solo letras/números/guiones
            var nombreLimpio = string.Join("-", nombreEmpresa.Split(new[] { ' ', '/', '\\', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => string.Join("", s.Where(char.IsLetterOrDigit))));

            // Asegurar que tenga un formato coherente
            if (string.IsNullOrEmpty(nombreLimpio))
                nombreLimpio = "Empresa";

            var empresa = new Empresa
            {
                NombreComercial = nombreLimpio,
                RazonSocial = nombreEmpresa,
                CIF = $"B{Guid.NewGuid():N}"[..10],
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
            var empresa = await _context.Empresas.FindAsync(id);

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