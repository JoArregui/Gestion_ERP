using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities;
using ERP.Domain.Entities.CanalEtico;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.CanalEtico
{
    /// <summary>
    /// Servicio de gestión del Canal Ético / Whistleblowing (Ley 2/2023).
    /// Protección del informante, confidencialidad, prohibición de represalias.
    /// </summary>
    public class CanalEticoService
    {
        private readonly ApplicationDbContext _context;

        public CanalEticoService(ApplicationDbContext context) => _context = context;

        // ── Configuración ─────────────────────────────────────────────
        public async Task<ConfiguracionCanalEtico> GetConfigAsync(int empresaId)
            => await _context.ConfiguracionesCanalEtico
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId)
                ?? new ConfiguracionCanalEtico { EmpresaId = empresaId };

        public async Task<ConfiguracionCanalEtico> UpsertConfigAsync(int empresaId, ConfiguracionCanalEtico dto)
        {
            var existing = await _context.ConfiguracionesCanalEtico
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);
            if (existing != null)
            {
                existing.CanalWebActivo = dto.CanalWebActivo;
                existing.CanalEmailActivo = dto.CanalEmailActivo;
                existing.EmailCanalEtico = dto.EmailCanalEtico;
                existing.CanalTelefonoActivo = dto.CanalTelefonoActivo;
                existing.TelefonoCanalEtico = dto.TelefonoCanalEtico;
                existing.CanalPresencialActivo = dto.CanalPresencialActivo;
                existing.CanalAppMovilActivo = dto.CanalAppMovilActivo;
                existing.DiasMaximosEvaluacion = dto.DiasMaximosEvaluacion;
                existing.DiasMaximosInvestigacion = dto.DiasMaximosInvestigacion;
                existing.PermiteAnonimas = dto.PermiteAnonimas;
                existing.RequiereConfirmacionRecepcion = dto.RequiereConfirmacionRecepcion;
                existing.NotificarInformanteCambios = dto.NotificarInformanteCambios;
                existing.MedidasProteccionAutomaticas = dto.MedidasProteccionAutomaticas;
                existing.ProhibicionRepresaliasExplicita = dto.ProhibicionRepresaliasExplicita;
                existing.ResponsableCanalId = dto.ResponsableCanalId;
                existing.ResponsableProteccionDatosId = dto.ResponsableProteccionDatosId;
                existing.ComiteEticoIds = dto.ComiteEticoIds;
                existing.TextoAcuseRecibo = dto.TextoAcuseRecibo;
                existing.TextoConfirmacionAnonimato = dto.TextoConfirmacionAnonimato;
                existing.FechaUltimaActualizacion = DateTime.UtcNow;
                existing.UsuarioUltimaActualizacion = "system";
            }
            else
            {
                dto.EmpresaId = dto.EmpresaId > 0 ? dto.EmpresaId : throw new ArgumentException("EmpresaId requerido");
                dto.FechaCreacion = DateTime.UtcNow;
                _context.ConfiguracionesCanalEtico.Add(dto);
            }
            await _context.SaveChangesAsync();
            return await _context.ConfiguracionesCanalEtico.FirstAsync(c => c.EmpresaId == empresaId);
        }

        // ── Denuncias ─────────────────────────────────────────────────
        public async Task<List<DenunciaCanalEtico>> ListarAsync(int empresaId, EstadoDenuncia? estado = null, DateTime? desde = null, DateTime? hasta = null, int take = 100)
        {
            var q = _context.DenunciasCanalEtico.Where(d => d.EmpresaId == empresaId);
            if (estado.HasValue) q = q.Where(d => d.Estado == estado.Value);
            if (desde.HasValue) q = q.Where(d => d.FechaRecepcion >= desde.Value);
            if (hasta.HasValue) q = q.Where(d => d.FechaRecepcion <= hasta.Value);
            return await q.OrderByDescending(d => d.FechaRecepcion).Take(take).ToListAsync();
        }

        public async Task<DenunciaCanalEtico?> ObtenerAsync(int empresaId, int id)
            => await _context.DenunciasCanalEtico
                .Include(d => d.Comentarios)
                .Include(d => d.Documentos)
                .Include(d => d.HistorialEstados)
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);

        public async Task<DenunciaCanalEtico> CrearAsync(DenunciaCanalEtico dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titulo)) throw new ArgumentException("Título obligatorio");
            if (string.IsNullOrWhiteSpace(dto.DescripcionHechos)) throw new ArgumentException("Descripción de hechos obligatoria");

            var config = await GetConfigAsync(dto.EmpresaId);
            if (!config.PermiteAnonimas && dto.EsAnonima)
                throw new InvalidOperationException("No se permiten denuncias anónimas según configuración");

            var n = await _context.DenunciasCanalEtico.CountAsync(d => d.EmpresaId == dto.EmpresaId);
            dto.Codigo = $"DEN-{DateTime.Now:yyyy}-{(n + 1):D5}";
            dto.Estado = EstadoDenuncia.Recibida;
            dto.FechaRecepcion = DateTime.UtcNow;
            dto.FechaCreacion = DateTime.UtcNow;

            _context.DenunciasCanalEtico.Add(dto);
            await _context.SaveChangesAsync();

            // Registrar en historial
            await RegistrarCambioEstadoAsync(dto.Id, EstadoDenuncia.Recibida, EstadoDenuncia.Recibida, "Creación automática", "system");

            // Enviar acuse de recibo si configurado
            if (!dto.EsAnonima && config.RequiereConfirmacionRecepcion && !string.IsNullOrWhiteSpace(dto.EmailInformante))
            {
                // TODO: Enviar email de acuse de recibo
            }

            return dto;
        }

        public async Task<DenunciaCanalEtico> CambiarEstadoAsync(int empresaId, int id, EstadoDenuncia nuevoEstado, string? motivo = null, string? usuarioId = null)
        {
            var denuncia = await _context.DenunciasCanalEtico
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Denuncia no encontrada");

            if (denuncia.Estado == EstadoDenuncia.Resuelta || denuncia.Estado == EstadoDenuncia.Archivado || denuncia.Estado == EstadoDenuncia.Derivada)
                throw new InvalidOperationException("La denuncia ya tiene un estado terminal");

            var estadoAnterior = denuncia.Estado;
            denuncia.Estado = nuevoEstado;
            denuncia.FechaUltimaActualizacion = DateTime.UtcNow;
            denuncia.UsuarioUltimaActualizacion = usuarioId;

            switch (nuevoEstado)
            {
                case EstadoDenuncia.Admitida:
                    denuncia.FechaAdmision = DateTime.UtcNow;
                    break;
                case EstadoDenuncia.EnInvestigacion:
                    denuncia.FechaInvestigacion = DateTime.UtcNow;
                    break;
                case EstadoDenuncia.Resuelta:
                case EstadoDenuncia.Archivado:
                case EstadoDenuncia.Derivada:
                    denuncia.FechaResolucion = DateTime.UtcNow;
                    break;
            }

            await RegistrarCambioEstadoAsync(id, estadoAnterior, nuevoEstado, motivo, usuarioId);
            await _context.SaveChangesAsync();
            return denuncia;
        }

        public async Task<DenunciaCanalEtico> AsignarInstructorAsync(int empresaId, int id, string instructorId)
        {
            var denuncia = await _context.DenunciasCanalEtico
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Denuncia no encontrada");

            denuncia.InstructorId = instructorId;
            denuncia.FechaUltimaActualizacion = DateTime.UtcNow;
            denuncia.UsuarioUltimaActualizacion = "system";
            await _context.SaveChangesAsync();
            return denuncia;
        }

        public async Task<DenunciaCanalEtico> RegistrarResolucionAsync(int empresaId, int id, string resolucion, string tipoResolucion, string? medidasCorrectoras = null, string? usuarioId = null)
        {
            var denuncia = await _context.DenunciasCanalEtico
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Denuncia no encontrada");

            denuncia.Estado = EstadoDenuncia.Resuelta;
            denuncia.Resolucion = resolucion;
            denuncia.TipoResolucion = tipoResolucion;
            denuncia.MedidasCorrectoras = medidasCorrectoras;
            denuncia.FechaResolucion = DateTime.UtcNow;
            denuncia.ResponsableResolucionId = usuarioId;
            denuncia.FechaUltimaActualizacion = DateTime.UtcNow;
            denuncia.UsuarioUltimaActualizacion = usuarioId;

            await RegistrarCambioEstadoAsync(id, denuncia.Estado, EstadoDenuncia.Resuelta, $"Resolución: {tipoResolucion}", usuarioId);
            await _context.SaveChangesAsync();
            return denuncia;
        }

        public async Task<DenunciaCanalEtico> RegistrarRepresaliaAsync(int empresaId, int id, string detalle, string? usuarioId = null)
        {
            var denuncia = await _context.DenunciasCanalEtico
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Denuncia no encontrada");

            denuncia.HaSidoRepresaliado = true;
            denuncia.DetalleRepresalias = detalle;
            denuncia.FechaUltimaActualizacion = DateTime.UtcNow;
            denuncia.UsuarioUltimaActualizacion = usuarioId;

            await _context.SaveChangesAsync();
            return denuncia;
        }

        // ── Comentarios ──────────────────────────────────────────────
        public async Task<ComentarioDenuncia> AddComentarioAsync(int empresaId, int denunciaId, ComentarioDenuncia dto)
        {
            var denuncia = await _context.DenunciasCanalEtico
                .FirstOrDefaultAsync(d => d.Id == denunciaId && d.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Denuncia no encontrada");

            dto.DenunciaId = denunciaId;
            dto.Fecha = DateTime.UtcNow;
            _context.ComentariosDenuncia.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Documentos ───────────────────────────────────────────────
        public async Task<DocumentoDenuncia> AddDocumentoAsync(int empresaId, int denunciaId, DocumentoDenuncia dto, byte[] contenido, string hashSha256)
        {
            var denuncia = await _context.DenunciasCanalEtico
                .FirstOrDefaultAsync(d => d.Id == denunciaId && d.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Denuncia no encontrada");

            dto.DenunciaId = denunciaId;
            dto.FechaSubida = DateTime.UtcNow;
            dto.TamanoBytes = contenido.Length;
            dto.HashSha256 = hashSha256;

            _context.DocumentosDenuncia.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Historial ────────────────────────────────────────────────
        private async Task RegistrarCambioEstadoAsync(int denunciaId, EstadoDenuncia anterior, EstadoDenuncia nuevo, string? motivo, string? usuarioId)
        {
            var historial = new HistoricoEstadoDenuncia
            {
                DenunciaId = denunciaId,
                EstadoAnterior = anterior,
                EstadoNuevo = nuevo,
                Motivo = motivo,
                UsuarioId = usuarioId,
                Fecha = DateTime.UtcNow
            };
            _context.HistorialEstadosDenuncia.Add(historial);
            await _context.SaveChangesAsync();
        }

        public async Task<ConfiguracionCanalEtico> UpsertConfigAsync(ConfiguracionCanalEtico dto)
        {
            var existing = await _context.ConfiguracionesCanalEtico
                .FirstOrDefaultAsync(c => c.EmpresaId == dto.EmpresaId);
            if (existing != null)
            {
                existing.CanalWebActivo = dto.CanalWebActivo;
                existing.CanalEmailActivo = dto.CanalEmailActivo;
                existing.EmailCanalEtico = dto.EmailCanalEtico;
                existing.CanalTelefonoActivo = dto.CanalTelefonoActivo;
                existing.TelefonoCanalEtico = dto.TelefonoCanalEtico;
                existing.CanalPresencialActivo = dto.CanalPresencialActivo;
                existing.CanalAppMovilActivo = dto.CanalAppMovilActivo;
                existing.DiasMaximosEvaluacion = dto.DiasMaximosEvaluacion;
                existing.DiasMaximosInvestigacion = dto.DiasMaximosInvestigacion;
                existing.PermiteAnonimas = dto.PermiteAnonimas;
                existing.RequiereConfirmacionRecepcion = dto.RequiereConfirmacionRecepcion;
                existing.NotificarInformanteCambios = dto.NotificarInformanteCambios;
                existing.MedidasProteccionAutomaticas = dto.MedidasProteccionAutomaticas;
                existing.ProhibicionRepresaliasExplicita = dto.ProhibicionRepresaliasExplicita;
                existing.ResponsableCanalId = dto.ResponsableCanalId;
                existing.ResponsableProteccionDatosId = dto.ResponsableProteccionDatosId;
                existing.ComiteEticoIds = dto.ComiteEticoIds;
                existing.TextoAcuseRecibo = dto.TextoAcuseRecibo;
                existing.TextoConfirmacionAnonimato = dto.TextoConfirmacionAnonimato;
                existing.FechaUltimaActualizacion = DateTime.UtcNow;
            }
            else
            {
                dto.FechaCreacion = DateTime.UtcNow;
                _context.ConfiguracionesCanalEtico.Add(dto);
            }
            await _context.SaveChangesAsync();
            return existing ?? dto;
        }

        // ── Estadísticas ─────────────────────────────────────────────
        public async Task<object> GetEstadisticasAsync(int empresaId, DateTime? desde = null, DateTime? hasta = null)
        {
            var q = _context.DenunciasCanalEtico.Where(d => d.EmpresaId == empresaId);
            if (desde.HasValue) q = q.Where(d => d.FechaRecepcion >= desde.Value);
            if (hasta.HasValue) q = q.Where(d => d.FechaRecepcion <= hasta.Value);

            var total = await q.CountAsync();
            var porEstado = await q.GroupBy(d => d.Estado).Select(g => new { Estado = g.Key.ToString(), Total = g.Count() }).ToListAsync();
            var porTipo = await q.Where(d => !string.IsNullOrEmpty(d.TipoInfraccion)).GroupBy(d => d.TipoInfraccion).Select(g => new { Tipo = g.Key, Total = g.Count() }).OrderByDescending(x => x.Total).ToListAsync();
            var porGravedad = await q.Where(d => !string.IsNullOrEmpty(d.Gravedad)).GroupBy(d => d.Gravedad).Select(g => new { Gravedad = g.Key, Total = g.Count() }).ToListAsync();
            var anonimas = await q.CountAsync(d => d.EsAnonima);
            var conRepresalias = await q.CountAsync(d => d.HaSidoRepresaliado);
            var tiempoMedioResolucion = await q.Where(d => d.Estado == EstadoDenuncia.Resuelta && d.FechaResolucion.HasValue)
                .AverageAsync(d => (d.FechaResolucion.Value - d.FechaRecepcion).TotalDays);

            return new
            {
                Total = total,
                Anonimas = anonimas,
                ConRepresalias = conRepresalias,
                TiempoMedioResolucionDias = Math.Round(tiempoMedioResolucion, 1),
                PorEstado = porEstado,
                PorTipoInfraccion = porTipo,
                PorGravedad = porGravedad
            };
        }
    }
}