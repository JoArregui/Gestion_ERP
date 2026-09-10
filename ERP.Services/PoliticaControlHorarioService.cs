using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    /// <summary>
    /// Servicio para la gestión de políticas de control horario por empresa.
    /// </summary>
    public class PoliticaControlHorarioService
    {
        private readonly ApplicationDbContext _context;

        public PoliticaControlHorarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la política de control horario de la empresa.
        /// </summary>
        public async Task<PoliticaControlHorario> ObtenerPoliticaAsync()
        {
            return await _context.PoliticasControlHorario.FirstOrDefaultAsync(p => p.EmpresaId != null);
        }

        /// <summary>
        /// Guarda o actualiza la política de control horario.
        /// </summary>
        public async Task GuardarPoliticaAsync(PoliticaControlHorario politica)
        {
            var existente = await _context.PoliticasControlHorario.FirstOrDefaultAsync(p => p.EmpresaId == politica.EmpresaId);
            if (existente == null)
            {
                _context.PoliticasControlHorario.Add(politica);
            }
            else
            {
                existente.RequiereGeolocalizacion = politica.RequiereGeolocalizacion;
                existente.PermiteAutoCorreccion = politica.PermiteAutoCorreccion;
                existente.MargenToleranciaMinutos = politica.MargenToleranciaMinutos;
                existente.HorasExtraMaxMes = politica.HorasExtraMaxMes;
                existente.RequiereFirmaCorreccion = politica.RequiereFirmaCorreccion;
                existente.AniosConservacion = politica.AniosConservacion;
                existente.ConvenioReferencia = politica.ConvenioReferencia;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Verifica si la empresa tiene política de control horario configurada.
        /// </summary>
        public async Task<bool> TienePoliticaAsync()
        {
            return await _context.PoliticasControlHorario.AnyAsync(p => p.EmpresaId != null);
        }
    }
}