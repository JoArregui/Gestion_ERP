using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Services
{
    public class NominaService
    {
        private readonly ApplicationDbContext _context;

        public NominaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Nomina> GenerarNominaMensual(int empleadoId, int mes, int anio)
        {
            // 1. Validación de existencia del empleado y su empresa
            var empleado = await _context.Empleados
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.Id == empleadoId);

            if (empleado == null) 
                throw new Exception($"El empleado con ID {empleadoId} no existe en el sistema.");

            // 2. Control de duplicados profesional
            var nominaExistente = await _context.Nominas
                .AnyAsync(n => n.EmpleadoId == empleadoId && n.Mes == mes && n.Anio == anio);

            if (nominaExistente) 
                throw new Exception($"Ya existe una nómina procesada para este empleado en el periodo {mes}/{anio}.");

            // 3. Procesamiento de Control Horario para cálculo de variables
            var primerDiaMes = new DateTime(anio, mes, 1);
            var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1);

            var fichajesDelMes = await _context.ControlesHorarios
                .Where(c => c.EmpleadoId == empleadoId && 
                            c.Entrada >= primerDiaMes && 
                            c.Entrada <= ultimoDiaMes && 
                            c.Salida.HasValue)
                .ToListAsync();

            decimal totalHorasRealizadas = (decimal)fichajesDelMes
                .Sum(f => (f.Salida!.Value - f.Entrada).TotalHours);

            // Lógica de Complementos: Jornada estándar 160h/mes. Horas extra a 20€.
            decimal importeHorasExtra = 0;
            if (totalHorasRealizadas > 160)
            {
                importeHorasExtra = (totalHorasRealizadas - 160) * 20;
            }

            // 4. Construcción de la Entidad Nómina
            var nuevaNomina = new Nomina
            {
                EmpleadoId = empleadoId,
                Mes = mes,
                Anio = anio,
                SalarioBase = empleado.SalarioBaseMensual,
                Complementos = importeHorasExtra,
                // Deducciones fijas del 15% (Seguridad Social + IRPF base)
                Deducciones = (empleado.SalarioBaseMensual + importeHorasExtra) * 0.15m,
                FechaEmision = DateTime.Now
            };

            _context.Nominas.Add(nuevaNomina);

            // 5. Integración con Tesorería: Generar obligación de pago
            decimal importeNeto = (nuevaNomina.SalarioBase + nuevaNomina.Complementos) - nuevaNomina.Deducciones;

            var obligacionPago = new Vencimiento
            {
                DocumentoId = null, // null indica gasto interno (Nómina) - evita FK a Documento 0
                FechaVencimiento = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes)),
                Importe = importeNeto,
                Estado = "Pendiente",
                MetodoPago = "Transferencia",
                EmpresaId = empleado.EmpresaId, 
                FechaPago = null
            };

            _context.Vencimientos.Add(obligacionPago);

            // 6. Persistencia atómica de datos
            await _context.SaveChangesAsync();

            return nuevaNomina;
        }

        public async Task<List<Nomina>> GetNominasAsync(int? empleadoId = null)
        {
            var q = _context.Nominas.Include(n => n.Empleado).AsQueryable();
            if (empleadoId.HasValue) q = q.Where(n => n.EmpleadoId == empleadoId.Value);
            return await q.OrderByDescending(n => n.Anio).ThenByDescending(n => n.Mes).ToListAsync();
        }

        public async Task<bool> GenerarRemesaSEPAAsync(int empleadoId)
        {
            var empleado = await _context.Empleados.FindAsync(empleadoId);
            if (empleado == null) return false;
            // Stub: generar remesa SEPA pain.001 simulada para la nómina pendiente
            var ultimoMes = DateTime.Now.Month;
            var ultimoAnio = DateTime.Now.Year;
            var nomina = await _context.Nominas.FirstOrDefaultAsync(n => n.EmpleadoId == empleadoId && n.Mes == ultimoMes && n.Anio == ultimoAnio);
            if (nomina == null) return false;
            return true;
        }

        public async Task<bool> ProcesarPagoNominaAsync(int nominaId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nomina = await _context.Nominas
                    .Include(n => n.Empleado)
                    .FirstOrDefaultAsync(n => n.Id == nominaId);

                // Corrección CS8602: Validar que nomina y nomina.Empleado no sean nulos
                if (nomina == null || nomina.Empleado == null || nomina.EstaPagada) return false;

                // Marcar como pagada en RRHH
                nomina.EstaPagada = true;

                // Buscar y actualizar el vencimiento asociado en Tesorería (DocumentoId null = nómina)
                var vencimiento = await _context.Vencimientos
                    .FirstOrDefaultAsync(v => v.DocumentoId == null 
                                         && v.EmpresaId == nomina.Empleado.EmpresaId 
                                         && v.FechaVencimiento.Month == nomina.Mes 
                                         && v.FechaVencimiento.Year == nomina.Anio
                                         && v.Importe == (nomina.SalarioBase + nomina.Complementos - nomina.Deducciones)
                                         && v.Estado == "Pendiente");

                if (vencimiento != null)
                {
                    vencimiento.Estado = "Pagado";
                    vencimiento.FechaPago = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}