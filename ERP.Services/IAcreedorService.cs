using ERP.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ERP.Services
{
    public interface IAcreedorService
    {
        Task<List<Acreedor>> GetAllAsync();
        Task<Acreedor> GetByIdAsync(int id);
        Task AddAsync(Acreedor acreedor);
        Task UpdateAsync(Acreedor acreedor);
        Task DeleteAsync(int id);
        Task<List<Acreedor>> GetByStatusAsync(bool isActivo);
    }
}