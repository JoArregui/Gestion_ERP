using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.DTOs.CRM;
using ERP.Domain.Entities;
using ERP.Domain.Entities.CRM;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.CRM
{
    /// <summary>
    /// CRM mínimo viable (Fase B flujo ERP.txt §2): leads, embudo y conversión a presupuesto.
    /// </summary>
    public class CrmService
    {
        private readonly ApplicationDbContext _context;
        private readonly CicloFacturacionService _ciclo;

        public CrmService(ApplicationDbContext context, CicloFacturacionService ciclo)
        {
            _context = context;
            _ciclo = ciclo;
        }

        // ── Leads ──────────────────────────────────────────────
        public async Task<List<Lead>> ListarLeadsAsync(int empresaId, EstadoLead? estado = null)
        {
            var q = _context.Leads.Where(l => l.EmpresaId == empresaId);
            if (estado.HasValue) q = q.Where(l => l.Estado == estado.Value);
            return await q.OrderByDescending(l => l.FechaCreacion).ToListAsync();
        }

        public async Task<Lead> CrearLeadAsync(Lead dto, string? usuario)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) throw new ArgumentException("Nombre obligatorio");
            dto.Estado = EstadoLead.Nuevo;
            dto.FechaCreacion = DateTime.Now;
            dto.UsuarioAsignado ??= usuario;
            _context.Leads.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<Lead> CambiarEstadoLeadAsync(int empresaId, int leadId, EstadoLead nuevo)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId && l.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Lead no encontrado");
            if (lead.Estado == EstadoLead.Convertido) throw new InvalidOperationException("Lead ya convertido");
            if (nuevo == EstadoLead.Convertido) throw new InvalidOperationException("Use ConvertirLead para convertir");
            lead.Estado = nuevo;
            if (nuevo == EstadoLead.Contactado && lead.FechaContacto == null) lead.FechaContacto = DateTime.Now;
            await _context.SaveChangesAsync();
            return lead;
        }

        /// <summary>Convierte el lead en cliente + oportunidad inicial (sin borrar el lead).</summary>
        public async Task<(Cliente cliente, Oportunidad oportunidad)> ConvertirLeadAsync(int empresaId, int leadId, string? usuario)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == leadId && l.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Lead no encontrado");
            if (lead.Estado == EstadoLead.Convertido) throw new InvalidOperationException("Lead ya convertido");

            var count = await _context.Clientes.CountAsync(c => c.EmpresaId == empresaId);
            var cliente = new Cliente
            {
                EmpresaId = empresaId,
                CodigoCliente = $"CLI-{(count + 1):D4}",
                RazonSocial = string.IsNullOrWhiteSpace(lead.EmpresaNombre) ? lead.Nombre : lead.EmpresaNombre,
                NombreComercial = lead.Nombre,
                CIF = string.IsNullOrWhiteSpace(lead.CIF) ? $"PLEAD-{lead.Id:D6}" : lead.CIF,
                Email = lead.Email,
                Telefono = lead.Telefono,
                PaisISO = "ES"
            };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            var op = new Oportunidad
            {
                EmpresaId = empresaId,
                Titulo = $"Oportunidad {cliente.RazonSocial}",
                LeadId = lead.Id,
                ClienteId = cliente.Id,
                Etapa = EtapaOportunidad.Prospeccion,
                Probabilidad = 10,
                UsuarioCreacion = usuario
            };
            _context.Oportunidades.Add(op);

            lead.Estado = EstadoLead.Convertido;
            lead.ClienteId = cliente.Id;
            lead.FechaConversion = DateTime.Now;
            await _context.SaveChangesAsync();
            return (cliente, op);
        }

        // ── Oportunidades / embudo ─────────────────────────────
        public async Task<List<Oportunidad>> ListarOportunidadesAsync(int empresaId, EtapaOportunidad? etapa = null)
        {
            var q = _context.Oportunidades.Include(o => o.Cliente).Include(o => o.Lead).Where(o => o.EmpresaId == empresaId);
            if (etapa.HasValue) q = q.Where(o => o.Etapa == etapa.Value);
            return await q.OrderByDescending(o => o.FechaCreacion).ToListAsync();
        }

        public async Task<Oportunidad> CrearOportunidadAsync(Oportunidad dto, string? usuario)
        {
            if (string.IsNullOrWhiteSpace(dto.Titulo)) throw new ArgumentException("Título obligatorio");
            if (dto.ImporteEstimado < 0) throw new ArgumentException("Importe no puede ser negativo");
            if (dto.ClienteId.HasValue && !await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId.Value && c.EmpresaId == dto.EmpresaId))
                throw new ArgumentException("Cliente no pertenece a la empresa");
            if (dto.LeadId.HasValue && !await _context.Leads.AnyAsync(l => l.Id == dto.LeadId.Value && l.EmpresaId == dto.EmpresaId))
                throw new ArgumentException("Lead no pertenece a la empresa");
            dto.Etapa = EtapaOportunidad.Prospeccion;
            dto.UsuarioCreacion = usuario;
            dto.FechaCreacion = DateTime.Now;
            _context.Oportunidades.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task<Oportunidad> CambiarEtapaAsync(int empresaId, int oportunidadId, EtapaOportunidad nueva, string? motivoPerdida = null)
        {
            var op = await _context.Oportunidades.FirstOrDefaultAsync(o => o.Id == oportunidadId && o.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Oportunidad no encontrada");
            if (op.Etapa == EtapaOportunidad.Ganada) throw new InvalidOperationException("Ganada es terminal");
            if (nueva == EtapaOportunidad.Ganada && op.Etapa != EtapaOportunidad.Negociacion)
                throw new InvalidOperationException("Solo se gana desde Negociación");
            if (nueva == EtapaOportunidad.Perdida && string.IsNullOrWhiteSpace(motivoPerdida))
                throw new ArgumentException("Motivo de pérdida obligatorio");
            if (op.Etapa == EtapaOportunidad.Perdida && nueva != EtapaOportunidad.Prospeccion)
                throw new InvalidOperationException("Perdida solo admite reapertura a Prospección");

            op.Etapa = nueva;
            if (nueva == EtapaOportunidad.Ganada) { op.FechaCierre = DateTime.Now; op.Probabilidad = 100; }
            else if (nueva == EtapaOportunidad.Perdida) { op.FechaCierre = DateTime.Now; op.Probabilidad = 0; op.MotivoPerdida = motivoPerdida; }
            else { op.FechaCierre = null; op.MotivoPerdida = null; }
            await _context.SaveChangesAsync();
            return op;
        }

        /// <summary>Genera el presupuesto (DocumentoComercial) desde la oportunidad ganada o en negociación.</summary>
        public async Task<DocumentoComercial> ConvertirAPresupuestoAsync(int empresaId, int oportunidadId, decimal? ivaPorciento = null)
        {
            var op = await _context.Oportunidades.Include(o => o.Cliente).FirstOrDefaultAsync(o => o.Id == oportunidadId && o.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Oportunidad no encontrada");
            if (op.Etapa != EtapaOportunidad.Negociacion && op.Etapa != EtapaOportunidad.Ganada)
                throw new InvalidOperationException("Solo Negociación/Ganada generan presupuesto");
            if (op.DocumentoPresupuestoId.HasValue) throw new InvalidOperationException("Ya tiene presupuesto generado");
            if (op.ImporteEstimado <= 0) throw new InvalidOperationException("Importe estimado debe ser > 0");

            var iva = ivaPorciento ?? 21m;
            var doc = new DocumentoComercial
            {
                EmpresaId = empresaId,
                Tipo = TipoDocumento.Presupuesto,
                EsCompra = false,
                Fecha = DateTime.Today,
                ClienteId = op.ClienteId,
                NumeroDocumento = string.Empty, // lo numera CrearDocumento
                Observaciones = $"Generado desde oportunidad #{op.Id} {op.Titulo}",
                Lineas = new List<DocumentoLinea>
                {
                    new() { DescripcionArticulo = op.Titulo, Cantidad = 1, PrecioUnitario = op.ImporteEstimado, PorcentajeIva = iva }
                }
            };
            var creado = await _ciclo.CrearDocumento(doc);
            op.DocumentoPresupuestoId = creado.Id;
            if (op.Etapa == EtapaOportunidad.Negociacion)
            {
                op.Etapa = EtapaOportunidad.Ganada;
                op.FechaCierre = DateTime.Now;
                op.Probabilidad = 100;
            }
            await _context.SaveChangesAsync();
            return creado;
        }

        public async Task<PipelineResumen> GetPipelineAsync(int empresaId)
        {
            var ops = await _context.Oportunidades.Where(o => o.EmpresaId == empresaId && o.Etapa != EtapaOportunidad.Perdida).ToListAsync();
            var etapas = Enum.GetValues<EtapaOportunidad>().Select(e => new EtapaResumen(
                e.ToString(),
                ops.Count(o => o.Etapa == e),
                ops.Where(o => o.Etapa == e).Sum(o => o.ImporteEstimado),
                Math.Round(ops.Where(o => o.Etapa == e).Sum(o => o.ImporteEstimado * o.Probabilidad / 100m), 2))).ToList();
            return new PipelineResumen(
                ops.Count,
                ops.Sum(o => o.ImporteEstimado),
                Math.Round(ops.Sum(o => o.ImporteEstimado * o.Probabilidad / 100m), 2),
                etapas);
        }
    }
}
