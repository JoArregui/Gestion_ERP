using ERP.Data;
using ERP.Domain.Entities.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    /// <summary>
    /// Servicio fiscal para configuraciones IVA/IGIC/IPSI y operaciones fiscales.
    /// </summary>
    public class FiscalService
    {
        private readonly ApplicationDbContext _context;

        public FiscalService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la tarifa IVA general por defecto (Península/Baleares).
        /// </summary>
        public async Task<TarifaImpuesto> GetTarifaIVAGeneralAsync()
        {
            return await _context.TarifasImpuesto.FirstOrDefaultAsync(t => t.Territorio == ERP.Domain.Entities.Fiscal.TerritorioFiscal.PeninsulaBaleares && t.TipoIVA == (int)ERP.Domain.Entities.Fiscal.TipoIVA.General);
        }

        /// <summary>
        /// Obtiene la tarifa IGIC general por defecto (Canarias).
        /// </summary>
        public async Task<TarifaImpuesto> GetTarifaIGICGeneralAsync()
        {
            return await _context.TarifasImpuesto.FirstOrDefaultAsync(t => t.Territorio == ERP.Domain.Entities.Fiscal.TerritorioFiscal.Canarias && t.TipoIVA == (int)ERP.Domain.Entities.Fiscal.TipoIVA.General);
        }

        /// <summary>
        /// Obtiene la tarifa IPSI por defecto (Ceuta/Melilla).
        /// </summary>
        public async Task<TarifaImpuesto> GetTarifaIPSIAsync()
        {
            return await _context.TarifasImpuesto.FirstOrDefaultAsync(t => t.Territorio == ERP.Domain.Entities.Fiscal.TerritorioFiscal.Ceuta);
        }

        /// <summary>
        /// Obtiene todas las tarifas activas.
        /// </summary>
        public async Task<List<TarifaImpuesto>> GetTarifasActivasAsync()
        {
            return await _context.TarifasImpuesto.Where(t => t.Vigente).ToListAsync();
        }

        /// <summary>
        /// Obtiene la configuración IVA de la empresa.
        /// </summary>
        public async Task<ConfiguracionIVA> GetConfiguracionIVAEmpresaAsync()
        {
            return await _context.ConfiguracionesIVA.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Guarda o actualiza la configuración IVA de la empresa.
        /// </summary>
        public async Task SetConfiguracionIVAEmpresaAsync(ConfiguracionIVA config)
        {
            var existente = await _context.ConfiguracionesIVA.FirstOrDefaultAsync(c => c.EmpresaId == config.EmpresaId && c.EjercicioId == config.EjercicioId);
            if (existente == null)
            {
                _context.ConfiguracionesIVA.Add(config);
            }
            else
            {
                existente.AplicaProrrataGeneral = config.AplicaProrrataGeneral;
                existente.PorcentajeProrrataGeneral = config.PorcentajeProrrataGeneral;
                existente.AplicaProrrataEspecial = config.AplicaProrrataEspecial;
                existente.TieneSectoresDiferenciados = config.TieneSectoresDiferenciados;
                existente.SujetoRecargoEquivalencia = config.SujetoRecargoEquivalencia;
                existente.RecargoGeneral = config.RecargoGeneral;
                existente.AplicaIVACaja = config.AplicaIVACaja;
                existente.InversionSujetoPasivoHabitual = config.InversionSujetoPasivoHabitual;
                existente.UsuarioModificacion = config.UsuarioModificacion;
                existente.FechaModificacion = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }
    }
}