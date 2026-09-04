using ERP.Data;
using ERP.Domain.Entities.RGPD;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    /// <summary>
    /// Servicio para el registro de actividades de tratamiento RGPD art.30.
    /// </summary>
    public class RegistroTratamientoService
    {
        private readonly ApplicationDbContext _context;

        public RegistroTratamientoService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las actividades de tratamiento.
        /// </summary>
        public async Task<List<RegistroTratamiento>> ListarAsync()
        {
            return await _context.RegistrosTratamiento.ToListAsync();
        }

        /// <summary>
        /// Obtiene una actividad por su ID.
        /// </summary>
        public async Task<RegistroTratamiento> ObtenerPorIdAsync(int id)
        {
            return await _context.RegistrosTratamiento.FindAsync(id);
        }

        /// <summary>
        /// Añade una nueva actividad de tratamiento.
        /// </summary>
        public async Task<AñadirResultado> AñadirAsync(RegistroTratamiento tratamiento)
        {
            _context.RegistrosTratamiento.Add(tratamiento);
            await _context.SaveChangesAsync();
            return new AñadirResultado { Exito = true };
        }

        /// <summary>
        /// Actualiza una actividad de tratamiento existente.
        /// </summary>
        public async Task<ActualizarResultado> ActualizarAsync(RegistroTratamiento tratamiento)
        {
            _context.Entry(tratamiento).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            await _context.SaveChangesAsync();
            return new ActualizarResultado { Exito = true };
        }

        /// <summary>
        /// Elimina una actividad de tratamiento.
        /// </summary>
        public async Task<EliminarResultado> EliminarAsync(int id)
        {
            var tratamiento = await _context.RegistrosTratamiento.FindAsync(id);
            if (tratamiento != null)
            {
                _context.RegistrosTratamiento.Remove(tratamiento);
                await _context.SaveChangesAsync();
            }
            return new EliminarResultado { Exito = true };
        }

        public class AñadirResultado
        {
            public bool Exito { get; set; }
        }

        public class ActualizarResultado
        {
            public bool Exito { get; set; }
        }

        public class EliminarResultado
        {
            public bool Exito { get; set; }
        }
    }
}