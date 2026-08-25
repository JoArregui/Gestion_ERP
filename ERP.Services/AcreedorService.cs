using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    public class AcreedorService : IAcreedorService
    {
        private readonly ApplicationDbContext _context;

        public AcreedorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Acreedor>> GetAllAsync()
        {
            return await _context.Acreedores
                .Where(a => a.IsActivo)
                .OrderBy(a => a.RazonSocial)
                .ToListAsync();
        }

        public async Task<Acreedor> GetByIdAsync(int id)
        {
            return await _context.Acreedores.FindAsync(id);
        }

        public async Task AddAsync(Acreedor acreedor)
        {
            _context.Acreedores.Add(acreedor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Acreedor acreedor)
        {
            _context.Entry(acreedor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var acreedor = await _context.Acreedores.FindAsync(id);
            if (acreedor != null)
            {
                acreedor.IsActivo = false;
                acreedor.FechaAlta = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Acreedor>> GetByStatusAsync(bool isActivo)
        {
            return await _context.Acreedores
                .Where(a => a.IsActivo == isActivo)
                .OrderBy(a => a.RazonSocial)
                .ToListAsync();
        }
    }
}