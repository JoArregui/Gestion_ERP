using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities.Transversal;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Transversal
{
    /// <summary>
    /// Servicio de consulta y configuración de auditoría (Fase G).
    /// </summary>
    public class AuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context) => _context = context;

        // ── Configuración ─────────────────────────────────────────────
        public async Task<List<AuditConfig>> GetConfigsAsync(int empresaId)
            => await _context.AuditConfigs.Where(c => c.EmpresaId == empresaId).OrderBy(c => c.Entidad).ToListAsync();

        public async Task<AuditConfig> UpsertConfigAsync(int empresaId, AuditConfig dto)
        {
            var existing = await _context.AuditConfigs
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Entidad == dto.Entidad);
            if (existing != null)
            {
                existing.PropsIgnoradas = dto.PropsIgnoradas;
                existing.PropsIncluidas = dto.PropsIncluidas;
                existing.AuditarInsert = dto.AuditarInsert;
                existing.AuditarUpdate = dto.AuditarUpdate;
                existing.AuditarDelete = dto.AuditarDelete;
                existing.Activa = dto.Activa;
            }
            else
            {
                dto.EmpresaId = empresaId;
                _context.AuditConfigs.Add(dto);
            }
            await _context.SaveChangesAsync();
            return existing ?? dto;
        }

        public async Task<bool> ToggleConfigAsync(int empresaId, string entidad, bool activa)
        {
            var c = await _context.AuditConfigs.FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Entidad == entidad);
            if (c == null) return false;
            c.Activa = activa;
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Consulta de logs ──────────────────────────────────────────
        public async Task<List<AuditLog>> GetLogsAsync(int empresaId, string? entidad = null, int? entidadId = null, DateTime? desde = null, DateTime? hasta = null, int take = 200)
        {
            var q = _context.AuditLogs.Where(a => a.EmpresaId == empresaId);
            if (!string.IsNullOrWhiteSpace(entidad)) q = q.Where(a => a.Entidad == entidad);
            if (entidadId.HasValue) q = q.Where(a => a.EntidadId == entidadId.Value);
            if (desde.HasValue) q = q.Where(a => a.Fecha >= desde.Value);
            if (hasta.HasValue) q = q.Where(a => a.Fecha <= hasta.Value);
            return await q.OrderByDescending(a => a.Fecha).Take(take).ToListAsync();
        }

        public async Task<object> GetResumenAsync(int empresaId, DateTime? desde = null)
        {
            var q = _context.AuditLogs.Where(a => a.EmpresaId == empresaId);
            if (desde.HasValue) q = q.Where(a => a.Fecha >= desde.Value);
            var total = await q.CountAsync();
            var porEntidad = await q.GroupBy(a => a.Entidad).Select(g => new { Entidad = g.Key, Total = g.Count() }).OrderByDescending(x => x.Total).ToListAsync();
            var porAccion = await q.GroupBy(a => a.Accion).Select(g => new { Accion = g.Key, Total = g.Count() }).ToListAsync();
            var porUsuario = await q.Where(a => !string.IsNullOrEmpty(a.UsuarioId)).GroupBy(a => a.UsuarioId).Select(g => new { Usuario = g.Key, Total = g.Count() }).OrderByDescending(x => x.Total).Take(10).ToListAsync();
            return new { Total = total, PorEntidad = porEntidad, PorAccion = porAccion, TopUsuarios = porUsuario };
        }
    }
}