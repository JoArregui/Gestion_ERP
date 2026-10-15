using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Proyectos;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Proyectos
{
    /// <summary>
    /// Proyectos / SAT Fase F: WBS/Gantt, Timesheets, Rentabilidad, Tickets.
    /// </summary>
    public class ProyectosService
    {
        private readonly ApplicationDbContext _context;

        public ProyectosService(ApplicationDbContext context) => _context = context;

        // ── Proyectos ────────────────────────────────────────────────
        public async Task<List<Proyecto>> ListarAsync(int empresaId, EstadoProyecto? estado = null)
        {
            var q = _context.Proyectos.Include(p => p.Cliente).Where(p => p.EmpresaId == empresaId);
            if (estado.HasValue) q = q.Where(p => p.Estado == estado.Value);
            return await q.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        }

        public async Task<Proyecto?> ObtenerAsync(int empresaId, int id)
            => await _context.Proyectos
                .Include(p => p.Cliente)
                .Include(p => p.Hitos)
                .Include(p => p.Tareas)
                .Include(p => p.Timesheets).ThenInclude(t => t.Empleado)
                .Include(p => p.Documentos)
                .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);

        public async Task<Proyecto> CrearAsync(Proyecto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) throw new ArgumentException("Nombre obligatorio");
            if (string.IsNullOrWhiteSpace(dto.Codigo))
            {
                var n = await _context.Proyectos.CountAsync(p => p.EmpresaId == dto.EmpresaId);
                dto.Codigo = $"PRY-{(dto.FechaCreacion.Year):D4}-{(n + 1):D4}";
            }
            if (await _context.Proyectos.AnyAsync(p => p.EmpresaId == dto.EmpresaId && p.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            dto.Estado = EstadoProyecto.Borrador;
            _context.Proyectos.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<Proyecto> CambiarEstadoAsync(int empresaId, int id, EstadoProyecto nuevo)
        {
            var p = await _context.Proyectos.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proyecto no encontrado");
            if (p.Estado == EstadoProyecto.Terminado && nuevo != EstadoProyecto.Terminado)
                throw new InvalidOperationException("Proyecto terminado solo puede reabrirse manualmente");
            p.Estado = nuevo;
            if (nuevo == EstadoProyecto.EnEjecucion && p.FechaInicioReal == null) p.FechaInicioReal = DateTime.Today;
            if (nuevo == EstadoProyecto.Terminado && p.FechaFinReal == null) p.FechaFinReal = DateTime.Today;
            await _context.SaveChangesAsync();
            return p;
        }

        // ── Hitos ───────────────────────────────────────────────────
        public async Task<HitoProyecto> AddHitoAsync(int empresaId, int proyectoId, HitoProyecto dto)
        {
            var p = await _context.Proyectos.FirstOrDefaultAsync(x => x.Id == proyectoId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proyecto no encontrado");
            dto.ProyectoId = proyectoId;
            if (dto.Orden == 0) dto.Orden = (await _context.HitosProyecto.CountAsync(h => h.ProyectoId == proyectoId)) + 1;
            _context.HitosProyecto.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Tareas / WBS ────────────────────────────────────────────
        public async Task<List<TareaProyecto>> GetTareasAsync(int empresaId, int proyectoId)
            => await _context.TareasProyecto
                .Include(t => t.Hito)
                .Include(t => t.TareaPredecesora)
                .Where(t => t.ProyectoId == proyectoId)
                .OrderBy(t => t.Orden).ToListAsync();

        public async Task<TareaProyecto> AddTareaAsync(int empresaId, int proyectoId, TareaProyecto dto)
        {
            var p = await _context.Proyectos.FirstOrDefaultAsync(x => x.Id == proyectoId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proyecto no encontrado");
            dto.ProyectoId = proyectoId;
            if (dto.Orden == 0) dto.Orden = (await _context.TareasProyecto.CountAsync(t => t.ProyectoId == proyectoId)) + 1;
            if (dto.TareaPredecesoraId.HasValue)
            {
                var pred = await _context.TareasProyecto.FindAsync(dto.TareaPredecesoraId.Value);
                if (pred == null || pred.ProyectoId != proyectoId) throw new ArgumentException("Predecesora no pertenece al proyecto");
            }
            _context.TareasProyecto.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<TareaProyecto> AvanzarTareaAsync(int empresaId, int tareaId, EstadoTarea nuevo, decimal? porcentaje = null)
        {
            var t = await _context.TareasProyecto.FirstOrDefaultAsync(x => x.Id == tareaId && x.Proyecto.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Tarea no encontrada");
            if (t.Estado == EstadoTarea.Completada && nuevo != EstadoTarea.Completada)
                throw new InvalidOperationException("Tarea completada solo se puede reabrir manualmente");
            t.Estado = nuevo;
            if (porcentaje.HasValue) t.PorcentajeAvance = Math.Clamp(porcentaje.Value, 0, 100);
            if (nuevo == EstadoTarea.EnCurso && t.FechaInicioReal == null) t.FechaInicioReal = DateTime.Today;
            if (nuevo == EstadoTarea.Completada && t.FechaFinReal == null) t.FechaFinReal = DateTime.Today;
            await _context.SaveChangesAsync();
            return t;
        }

        // ── Timesheets / Imputación ─────────────────────────────────
        public async Task<TimesheetProyecto> ImputarHorasAsync(int empresaId, TimesheetProyecto dto)
        {
            if (dto.Horas <= 0) throw new ArgumentException("Horas debe ser > 0");
            var p = await _context.Proyectos.FirstOrDefaultAsync(x => x.Id == dto.ProyectoId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proyecto no encontrado");
            if (dto.TareaId.HasValue)
            {
                var t = await _context.TareasProyecto.FirstOrDefaultAsync(x => x.Id == dto.TareaId.Value && x.ProyectoId == dto.ProyectoId)
                    ?? throw new KeyNotFoundException("Tarea no pertenece al proyecto");
            }
            var emp = await _context.Empleados.FindAsync(dto.EmpleadoId)
                ?? throw new KeyNotFoundException("Empleado no encontrado");
            dto.EmpresaId = empresaId;
            dto.CosteHora = dto.CosteHora > 0 ? dto.CosteHora : (emp.SalarioBaseMensual / 173m); // 173h mes aprox
            _context.TimesheetsProyecto.Add(dto);
            await _context.SaveChangesAsync();

            // Actualizar coste real tarea
            if (dto.TareaId.HasValue)
            {
                var t = await _context.TareasProyecto.FindAsync(dto.TareaId.Value);
                if (t != null)
                {
                    t.HorasReales += dto.Horas;
                    t.CosteRealHoras += dto.Horas * dto.CosteHora;
                    await _context.SaveChangesAsync();
                }
            }
            return dto;
        }

        public async Task<List<TimesheetProyecto>> GetTimesheetsAsync(int empresaId, int proyectoId, DateTime? desde = null, DateTime? hasta = null)
        {
            var q = _context.TimesheetsProyecto.Include(t => t.Empleado).Include(t => t.Tarea)
                .Where(t => t.EmpresaId == empresaId && t.ProyectoId == proyectoId);
            if (desde.HasValue) q = q.Where(t => t.Fecha >= desde.Value);
            if (hasta.HasValue) q = q.Where(t => t.Fecha <= hasta.Value);
            return await q.OrderByDescending(t => t.Fecha).ToListAsync();
        }

        // ── Rentabilidad ────────────────────────────────────────────
        public async Task<object> GetRentabilidadAsync(int empresaId, int proyectoId)
        {
            var p = await _context.Proyectos
                .Include(p => p.Timesheets)
                .Include(p => p.Tareas)
                .FirstOrDefaultAsync(x => x.Id == proyectoId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Proyecto no encontrado");

            var costeHoras = p.Timesheets.Sum(t => t.Horas * t.CosteHora);
            var costeMaterial = p.Tareas.Sum(t => t.CosteRealMaterial);
            var costeTotalReal = costeHoras + costeMaterial;
            var margen = p.FacturadoAcumulado - costeTotalReal;
            var margenPct = p.FacturadoAcumulado > 0 ? Math.Round(margen / p.FacturadoAcumulado * 100m, 2) : 0;

            return new
            {
                p.Codigo,
                p.Nombre,
                PresupuestoCoste = p.PresupuestoCosteTotal,
                PresupuestoVenta = p.PresupuestoVentaTotal,
                CosteReal = costeTotalReal,
                Facturado = p.FacturadoAcumulado,
                Margen = margen,
                MargenPct = margenPct,
                DesviacionCoste = costeTotalReal - p.PresupuestoCosteTotal,
                AvanceTareas = p.Tareas.Any() ? Math.Round(p.Tareas.Average(t => t.PorcentajeAvance), 1) : 0
            };
        }

        // ── SAT / Tickets ──────────────────────────────────────────
        public async Task<List<TicketSAT>> ListarTicketsAsync(int empresaId, EstadoTicket? estado = null, int? clienteId = null)
        {
            var q = _context.TicketsSAT.Include(t => t.Cliente).Include(t => t.Contrato).Include(t => t.Proyecto)
                .Where(t => t.EmpresaId == empresaId);
            if (estado.HasValue) q = q.Where(t => t.Estado == estado.Value);
            if (clienteId.HasValue) q = q.Where(t => t.ClienteId == clienteId.Value);
            return await q.OrderByDescending(t => t.FechaApertura).ToListAsync();
        }

        public async Task<TicketSAT> CrearTicketAsync(TicketSAT dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Asunto)) throw new ArgumentException("Asunto obligatorio");
            if (string.IsNullOrWhiteSpace(dto.NumeroTicket))
            {
                var n = await _context.TicketsSAT.CountAsync(t => t.EmpresaId == dto.EmpresaId);
                dto.NumeroTicket = $"SAT-{(DateTime.Now.Year):D4}-{(n + 1):D4}";
            }
            if (await _context.TicketsSAT.AnyAsync(t => t.EmpresaId == dto.EmpresaId && t.NumeroTicket == dto.NumeroTicket))
                throw new InvalidOperationException($"Número {dto.NumeroTicket} ya existe");
            dto.Estado = EstadoTicket.Abierto;
            dto.FechaApertura = DateTime.Now;
            // Calcular SLA según contrato
            if (dto.ContratoMantenimientoId.HasValue)
            {
                var c = await _context.ContratosMantenimiento.FindAsync(dto.ContratoMantenimientoId.Value);
                if (c != null)
                {
                    var horas = c.SLA switch { TipoSLA.Basico => 8, TipoSLA.Estandar => 4, TipoSLA.Prioritario => 2, TipoSLA.Critico => 1, _ => 8 };
                    dto.SLAFechaLimiteRespuesta = DateTime.Now.AddHours(horas);
                    dto.SLAFechaLimiteResolucion = DateTime.Now.AddHours(horas * 4);
                }
            }
            _context.TicketsSAT.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<TicketSAT> CambiarEstadoTicketAsync(int empresaId, int ticketId, EstadoTicket nuevo, string? tecnicoId = null, string? solucion = null)
        {
            var t = await _context.TicketsSAT.FirstOrDefaultAsync(x => x.Id == ticketId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Ticket no encontrado");
            if (t.Estado == EstadoTicket.Cerrado && nuevo != EstadoTicket.Cerrado)
                throw new InvalidOperationException("Ticket cerrado solo se puede reabrir manualmente");
            t.Estado = nuevo;
            if (nuevo == EstadoTicket.Asignado && string.IsNullOrEmpty(t.TecnicoAsignadoId) && !string.IsNullOrEmpty(tecnicoId))
                t.TecnicoAsignadoId = tecnicoId;
            if (nuevo == EstadoTicket.EnProceso && t.FechaInicioTrabajo == null) t.FechaInicioTrabajo = DateTime.Now;
            if (nuevo == EstadoTicket.Resuelto) { t.FechaResolucion = DateTime.Now; if (!string.IsNullOrEmpty(solucion)) t.Solucion = solucion; }
            if (nuevo == EstadoTicket.Cerrado) { t.FechaCierre = DateTime.Now; t.Solucion = solucion ?? t.Solucion; }
            // SLA check
            if (t.SLAFechaLimiteRespuesta.HasValue && DateTime.Now > t.SLAFechaLimiteRespuesta.Value && t.Estado != EstadoTicket.Cerrado) t.SLAIncumplido = true;
            await _context.SaveChangesAsync();
            return t;
        }

        public async Task<TicketComentario> AddComentarioAsync(int empresaId, int ticketId, int empleadoId, string comentario, bool interno = true)
        {
            var t = await _context.TicketsSAT.FirstOrDefaultAsync(x => x.Id == ticketId && x.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Ticket no encontrado");
            var c = new TicketComentario { TicketId = ticketId, EmpleadoId = empleadoId, Comentario = comentario, EsInterno = interno };
            _context.TicketComentarios.Add(c);
            await _context.SaveChangesAsync();
            return c;
        }

        // ── Contratos Mantenimiento ─────────────────────────────────
        public async Task<List<ContratoMantenimiento>> GetContratosAsync(int empresaId, bool soloActivos = true)
        {
            var q = _context.ContratosMantenimiento.Include(c => c.Cliente).Where(c => c.EmpresaId == empresaId);
            if (soloActivos) q = q.Where(c => c.Activo);
            return await q.OrderBy(c => c.Codigo).ToListAsync();
        }

        public async Task<ContratoMantenimiento> CrearContratoAsync(ContratoMantenimiento dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo))
            {
                var n = await _context.ContratosMantenimiento.CountAsync(c => c.EmpresaId == dto.EmpresaId);
                dto.Codigo = $"MAN-{(n + 1):D4}";
            }
            if (await _context.ContratosMantenimiento.AnyAsync(c => c.EmpresaId == dto.EmpresaId && c.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            _context.ContratosMantenimiento.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }
    }
}