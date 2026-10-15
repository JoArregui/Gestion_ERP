using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.Transversal;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Transversal
{
    /// <summary>
    /// Motor de workflows genérico (Fase G §9).
    /// Evalúa reglas al crear/actualizar entidades y ejecuta acciones (aprobar, notificar, crear tarea, cambiar estado).
    /// </summary>
    public class WorkflowService
    {
        private readonly ApplicationDbContext _context;

        public WorkflowService(ApplicationDbContext context) => _context = context;

        // ── Definiciones ─────────────────────────────────────────────
        public async Task<List<WorkflowDefinicion>> ListarDefinicionesAsync(int empresaId)
            => await _context.WorkflowDefiniciones.Where(w => w.EmpresaId == empresaId).OrderBy(w => w.Prioridad).ToListAsync();

        public async Task<WorkflowDefinicion> CrearDefinicionAsync(WorkflowDefinicion dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo)) throw new ArgumentException("Código obligatorio");
            if (string.IsNullOrWhiteSpace(dto.EntidadObjetivo)) throw new ArgumentException("Entidad objetivo obligatoria");
            if (await _context.WorkflowDefiniciones.AnyAsync(w => w.EmpresaId == dto.EmpresaId && w.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            _context.WorkflowDefiniciones.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Evaluación y ejecución ───────────────────────────────────
        /// <summary>
        /// Punto de entrada: llamar tras SaveChanges en controladores/servicios.
        /// </summary>
        public async Task<List<WorkflowInstancia>> EvaluarYEjecutarAsync(int empresaId, string entidad, int entidadId, string accion, object? entidadObj = null)
        {
            var defs = await _context.WorkflowDefiniciones
                .Where(w => w.EmpresaId == empresaId && w.Activo && w.EntidadObjetivo == entidad)
                .OrderBy(w => w.Prioridad)
                .ToListAsync();

            var resultados = new List<WorkflowInstancia>();

            foreach (var def in defs)
            {
                if (!CoincideAccion(def, accion)) continue;
                if (!await EvaluarCondicionAsync(def, entidadObj)) continue;

                var instancia = new WorkflowInstancia
                {
                    EmpresaId = empresaId,
                    WorkflowDefinicionId = def.Id,
                    EntidadObjetivo = entidad,
                    EntidadObjetivoId = entidadId,
                    Estado = EstadoWorkflowInstancia.EnEjecucion,
                    FechaInicio = DateTime.Now
                };
                _context.WorkflowInstancias.Add(instancia);
                await _context.SaveChangesAsync(); // Guardar para tener Id

                // Crear pasos desde AccionesJson
                var pasos = CrearPasosDesdeDefinicion(def, instancia.Id);
                _context.WorkflowPasos.AddRange(pasos);
                await _context.SaveChangesAsync();

                // Ejecutar pasos automáticos (los que no requieren intervención humana)
                await EjecutarPasosAutomaticosAsync(instancia.Id, entidad, entidadId);

                // Recargar y determinar estado final
                var inst = await _context.WorkflowInstancias
                    .Include(i => i.Pasos)
                    .FirstAsync(w => w.Id == instancia.Id);

                inst.Estado = inst.Pasos.All(p => p.Estado == EstadoWorkflowPaso.Completado || p.Estado == EstadoWorkflowPaso.Saltado)
                    ? EstadoWorkflowInstancia.Completado
                    : inst.Pasos.Any(p => p.Estado == EstadoWorkflowPaso.Fallido)
                        ? EstadoWorkflowInstancia.Fallido
                        : EstadoWorkflowInstancia.EnEjecucion;
                inst.FechaFin = inst.Estado == EstadoWorkflowInstancia.Completado || inst.Estado == EstadoWorkflowInstancia.Fallido ? DateTime.Now : null;

                await _context.SaveChangesAsync();
                resultados.Add(inst);
            }

            return resultados;
        }

        private static bool CoincideAccion(WorkflowDefinicion def, string accion)
        {
            if (string.IsNullOrWhiteSpace(def.AccionDisparadora)) return true; // Sin filtro = siempre
            return def.AccionDisparadora.Equals(accion, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> EvaluarCondicionAsync(WorkflowDefinicion def, object? entidadObj)
        {
            if (string.IsNullOrWhiteSpace(def.CondicionJson)) return true;

            try
            {
                var cond = JsonSerializer.Deserialize<CondicionWorkflow>(def.CondicionJson);
                if (cond == null || entidadObj == null) return false;

                var valor = ObtenerValorPropiedad(entidadObj, cond.Campo);
                if (valor == null) return false;

                return EvaluarOperador(valor, cond.Operador, cond.Valor);
            }
            catch { return false; }
        }

        private static bool EvaluarOperador(object valor, string operador, object? esperado)
        {
            if (esperado == null) return false;
            var v = Convert.ToDecimal(valor);
            var e = Convert.ToDecimal(esperado);
            return operador switch
            {
                ">" => v > e,
                ">=" => v >= e,
                "<" => v < e,
                "<=" => v <= e,
                "==" => v == e,
                "!=" => v != e,
                "contiene" => valor.ToString()?.Contains(esperado.ToString() ?? "", StringComparison.OrdinalIgnoreCase) ?? false,
                _ => false
            };
        }

        private static object? ObtenerValorPropiedad(object obj, string propiedad)
        {
            var prop = obj.GetType().GetProperty(propiedad);
            return prop?.GetValue(obj);
        }

        private class CondicionWorkflow
        {
            public string Campo { get; set; } = "";
            public string Operador { get; set; } = "";
            public object? Valor { get; set; }
        }

        private class AccionWorkflow
        {
            public string Tipo { get; set; } = "";
            public Dictionary<string, object>? Parametros { get; set; }
        }

        private List<WorkflowPaso> CrearPasosDesdeDefinicion(WorkflowDefinicion def, int instanciaId)
        {
            if (string.IsNullOrWhiteSpace(def.AccionesJson)) return new List<WorkflowPaso>();

            try
            {
                var acciones = JsonSerializer.Deserialize<List<AccionWorkflow>>(def.AccionesJson) ?? new List<AccionWorkflow>();
                var pasos = new List<WorkflowPaso>();
                for (int i = 0; i < acciones.Count; i++)
                {
                    var a = acciones[i];
                    pasos.Add(new WorkflowPaso
                    {
                        InstanciaId = instanciaId,
                        Orden = i + 1,
                        TipoAccion = a.Tipo,
                        ParametrosJson = JsonSerializer.Serialize(a.Parametros),
                        Estado = EsAutomatico(a.Tipo) ? EstadoWorkflowPaso.Pendiente : EstadoWorkflowPaso.Pendiente // El motor decide
                    });
                }
                return pasos;
            }
            catch { return new List<WorkflowPaso>(); }
        }

        private static bool EsAutomatico(string tipo)
            => tipo is "NOTIFICAR" or "CAMBIAR_ESTADO" or "CREAR_TAREA"; // AUTO

        private async Task EjecutarPasosAutomaticosAsync(int instanciaId, string entidad, int entidadId)
        {
            var pasos = await _context.WorkflowPasos
                .Where(p => p.InstanciaId == instanciaId && EsAutomatico(p.TipoAccion) && p.Estado == EstadoWorkflowPaso.Pendiente)
                .OrderBy(p => p.Orden)
                .ToListAsync();

            foreach (var paso in pasos)
            {
                paso.Estado = EstadoWorkflowPaso.Ejecutando;
                paso.FechaEjecucion = DateTime.Now;
                try
                {
                    var resultado = await EjecutarAccionAsync(paso, entidad, entidadId);
                    paso.Estado = EstadoWorkflowPaso.Completado;
                    paso.ResultadoJson = JsonSerializer.Serialize(resultado);
                }
                catch (Exception ex)
                {
                    paso.Estado = EstadoWorkflowPaso.Fallido;
                    paso.ErrorJson = ex.Message;
                }
                _context.WorkflowPasos.Update(paso);
            }
            await _context.SaveChangesAsync();
        }

        private async Task<object> EjecutarAccionAsync(WorkflowPaso paso, string entidad, int entidadId)
        {
            var param = string.IsNullOrWhiteSpace(paso.ParametrosJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(paso.ParametrosJson) ?? new();

            return paso.TipoAccion switch
            {
                "NOTIFICAR" => await EnviarNotificacionAsync(param),
                "CAMBIAR_ESTADO" => await CambiarEstadoEntidadAsync(entidad, entidadId, param),
                "CREAR_TAREA" => await CrearTareaAutomaticaAsync(param),
                _ => new { Mensaje = $"Acción {paso.TipoAccion} no implementada" }
            };
        }

        private async Task<object> EnviarNotificacionAsync(Dictionary<string, object> param)
        {
            // Buscar plantilla
            var codigo = param.GetValueOrDefault("plantilla")?.ToString();
            var destinatario = param.GetValueOrDefault("destinatario")?.ToString();
            if (string.IsNullOrWhiteSpace(codigo)) return new { Ok = false, Error = "Plantilla no especificada" };

            // TODO: Implementar envío real (EmailService, SignalR, etc.)
            return new { Ok = true, Plantilla = codigo, Destinatario = destinatario };
        }

        private async Task<object> CambiarEstadoEntidadAsync(string entidad, int entidadId, Dictionary<string, object> param)
        {
            var nuevoEstado = param.GetValueOrDefault("estado")?.ToString();
            if (string.IsNullOrWhiteSpace(nuevoEstado)) return new { Ok = false, Error = "Estado no especificado" };

            // TODO: Implementar según entidad (DocumentoComercial, OrdenFabricacion, etc.)
            return new { Ok = true, Entidad = entidad, Id = entidadId, NuevoEstado = nuevoEstado };
        }

        private async Task<object> CrearTareaAutomaticaAsync(Dictionary<string, object> param)
        {
            // TODO: Crear TareaProyecto o Tarea genérica
            return new { Ok = true, Titulo = param.GetValueOrDefault("titulo")?.ToString() ?? "Tarea automática" };
        }

        // ── Plantillas notificación ──────────────────────────────────
        public async Task<List<NotificacionPlantilla>> GetPlantillasAsync(int empresaId)
            => await _context.NotificacionPlantillas.Where(n => n.EmpresaId == empresaId && n.Activa).OrderBy(n => n.Codigo).ToListAsync();

        public async Task<NotificacionPlantilla> UpsertPlantillaAsync(NotificacionPlantilla dto)
        {
            var existing = await _context.NotificacionPlantillas.FirstOrDefaultAsync(n => n.EmpresaId == dto.EmpresaId && n.Codigo == dto.Codigo);
            if (existing != null)
            {
                existing.Nombre = dto.Nombre;
                existing.Asunto = dto.Asunto;
                existing.CuerpoHtml = dto.CuerpoHtml;
                existing.Canal = dto.Canal;
                existing.Activa = dto.Activa;
            }
            else
            {
                _context.NotificacionPlantillas.Add(dto);
            }
            await _context.SaveChangesAsync();
            return existing ?? dto;
        }
    }
}