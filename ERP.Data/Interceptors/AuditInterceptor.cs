using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ERP.Domain.Entities.Transversal;

namespace ERP.Data.Interceptors
{
    /// <summary>
    /// Interceptor EF Core para auditoría automática (Fase G).
    /// Se ejecuta en SaveChangesAsync y genera AuditLog por cada entidad con AuditConfig activa.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ITenantContext? _tenantContext;

        public AuditInterceptor(ITenantContext? tenantContext = null)
        {
            _tenantContext = tenantContext;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is ApplicationDbContext ctx)
            {
                await GenerarAuditLogsAsync(ctx, cancellationToken);
            }
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private async Task GenerarAuditLogsAsync(ApplicationDbContext ctx, CancellationToken ct)
        {
            var empresaId = _tenantContext?.EmpresaId ?? 0;
            if (empresaId == 0) return; // Sin empresa (seed, migraciones) no auditar

            // Cargar configs de auditoría para esta empresa (cacheable en producción)
            var configs = await ctx.AuditConfigs
                .Where(c => c.EmpresaId == empresaId && c.Activa)
                .ToListAsync(ct);

            if (!configs.Any()) return;

            var entries = ctx.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            var logs = new List<AuditLog>();
            var now = DateTime.UtcNow;
            var usuarioId = ObtenerUsuarioId(ctx);
            var usuarioNombre = ObtenerUsuarioNombre(ctx);
            var ip = ObtenerIp(ctx);

            foreach (var entry in entries)
            {
                var tipoEntidad = entry.Entity.GetType().Name;
                var config = configs.FirstOrDefault(c =>
                    (c.Entidad == "*" || c.Entidad.Equals(tipoEntidad, StringComparison.OrdinalIgnoreCase)) &&
                    CoincideAccion(c, entry.State));

                if (config == null) continue;

                var pk = ObtenerClavePrimaria(entry);
                if (pk == null) continue;

                var antes = entry.State != EntityState.Added
                    ? SerializarValores(entry, true, config)
                    : null;
                var despues = entry.State != EntityState.Deleted
                    ? SerializarValores(entry, false, config)
                    : null;

                // Si no hay cambios reales en UPDATE, saltar
                if (entry.State == EntityState.Modified && JsonSerializer.Serialize(antes) == JsonSerializer.Serialize(despues))
                    continue;

                var diff = entry.State == EntityState.Modified
                    ? CalcularDiferencia(antes, despues)
                    : null;

                logs.Add(new AuditLog
                {
                    EmpresaId = empresaId,
                    Entidad = tipoEntidad,
                    EntidadId = Convert.ToInt32(pk),
                    Accion = entry.State.ToString().ToUpperInvariant(),
                    UsuarioId = usuarioId,
                    UsuarioNombre = usuarioNombre,
                    Fecha = now,
                    Ip = ip,
                    ValoresAntes = antes,
                    ValoresDespues = despues,
                    Diferencia = diff
                });
            }

            if (logs.Any())
            {
                ctx.AuditLogs.AddRange(logs);
            }
        }

        private static bool CoincideAccion(AuditConfig config, EntityState state)
            => (state == EntityState.Added && config.AuditarInsert) ||
               (state == EntityState.Modified && config.AuditarUpdate) ||
               (state == EntityState.Deleted && config.AuditarDelete);

        private static object? ObtenerClavePrimaria(EntityEntry entry)
        {
            var pk = entry.Metadata.FindPrimaryKey();
            if (pk == null || pk.Properties.Count != 1) return null;
            var prop = pk.Properties[0];
            return entry.Property(prop.Name).CurrentValue;
        }

        private static string? SerializarValores(EntityEntry entry, bool valoresOriginales, AuditConfig config)
        {
            var dict = new Dictionary<string, object?>();
            var propsIgnoradas = new HashSet<string>(JsonSerializer.Deserialize<string[]>(config.PropsIgnoradas ?? "[]") ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var propsIncluidas = config.PropsIncluidas != null
                ? new HashSet<string>(JsonSerializer.Deserialize<string[]>(config.PropsIncluidas) ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase)
                : null;

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsPrimaryKey()) continue; // PK no se audita como valor
                if (propsIgnoradas.Contains(prop.Metadata.Name)) continue;
                if (propsIncluidas != null && !propsIncluidas.Contains(prop.Metadata.Name)) continue;

                var valor = valoresOriginales ? prop.OriginalValue : prop.CurrentValue;
                dict[prop.Metadata.Name] = valor;
            }

            return dict.Any() ? JsonSerializer.Serialize(dict) : null;
        }

        private static string? CalcularDiferencia(string? antes, string? despues)
        {
            if (string.IsNullOrEmpty(antes) || string.IsNullOrEmpty(despues)) return null;
            try
            {
                var a = JsonSerializer.Deserialize<Dictionary<string, object>>(antes);
                var d = JsonSerializer.Deserialize<Dictionary<string, object>>(despues);
                var diff = new Dictionary<string, object>();
                foreach (var kv in d)
                {
                    if (!a.TryGetValue(kv.Key, out var v) || !Equals(v, kv.Value))
                        diff[kv.Key] = new { Antes = a.GetValueOrDefault(kv.Key), Despues = kv.Value };
                }
                return diff.Any() ? JsonSerializer.Serialize(diff) : null;
            }
            catch { return null; }
        }

        private static string? ObtenerUsuarioId(ApplicationDbContext ctx)
        {
            // Intenta leer del HttpContext via ITenantContext o similar
            return null; // TODO: inyectar IHttpContextAccessor si está disponible
        }

        private static string? ObtenerUsuarioNombre(ApplicationDbContext ctx) => null;

        private static string? ObtenerIp(ApplicationDbContext ctx) => null;
    }
}