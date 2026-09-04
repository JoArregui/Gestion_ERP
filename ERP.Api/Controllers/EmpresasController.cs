using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmpresasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmpresasController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Crea la primera empresa durante el onboarding inicial
        /// </summary>
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
                SerieFacturacion = DateTime.Now.Year.ToString(),
                IvaDefecto = 21m,
                IsActiva = true,
                ColorHex = "#3498db",
                Eslogan = null,
                LogoUrl = null,
                LogoBase64 = null,
                FechaAlta = DateTime.Now,
                UltimaModificacion = null,
                TerritorioFiscal = ERP.Domain.Entities.Fiscal.TerritorioFiscal.PeninsulaBaleares,
                EsSII = false
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
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
                empresa.FechaAlta = DateTime.Now;
                empresa.UltimaModificacion = null;
                
                _context.Empresas.Add(empresa);
                await _context.SaveChangesAsync();

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
            empresa.UltimaModificacion = DateTime.Now;

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
            empresa.UltimaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmpresaExists(int id)
        {
            return _context.Empresas.Any(e => e.Id == id);
        }
    }
}