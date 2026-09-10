using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Services
{
    public class RRHHService
    {
        private readonly ApplicationDbContext _context;

        public RRHHService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RegistrarEntradaConPin(int empleadoId, string pin)
        {
            // Validar que el empleado existe y el PIN es correcto
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.Id == empleadoId && e.PinAcceso == pin);

            if (empleado == null) return false;

            // Evitar duplicar entrada si ya tiene una abierta
            var yaFichado = await _context.ControlesHorarios
                .AnyAsync(c => c.EmpleadoId == empleadoId && c.Salida == null);
            
            if (yaFichado) throw new Exception("Ya existe una jornada activa para este empleado.");

            var registro = new ControlHorario
            {
                EmpleadoId = empleadoId,
                Entrada = DateTime.Now
            };

            _context.ControlesHorarios.Add(registro);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RegistrarSalidaConPin(int empleadoId, string pin)
        {
            // Validar que el empleado existe y el PIN es correcto
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.Id == empleadoId && e.PinAcceso == pin);

            if (empleado == null) return false;

            var ultimoRegistro = await _context.ControlesHorarios
                .Where(c => c.EmpleadoId == empleadoId && c.Salida == null)
                .OrderByDescending(c => c.Entrada)
                .FirstOrDefaultAsync();

            if (ultimoRegistro != null)
            {
                ultimoRegistro.Salida = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        // Métodos legacy mantenidos por compatibilidad
        public async Task RegistrarEntrada(int empleadoId)
        {
            var registro = new ControlHorario
            {
                EmpleadoId = empleadoId,
                Entrada = DateTime.Now
            };

            _context.ControlesHorarios.Add(registro);
            await _context.SaveChangesAsync();
        }

        public async Task RegistrarSalida(int empleadoId)
        {
            var ultimoRegistro = await _context.ControlesHorarios
                .Where(c => c.EmpleadoId == empleadoId && c.Salida == null)
                .OrderByDescending(c => c.Entrada)
                .FirstOrDefaultAsync();

            if (ultimoRegistro != null)
            {
                ultimoRegistro.Salida = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
    }
}