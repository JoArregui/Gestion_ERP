using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stock-bajo")]
        public async Task<IActionResult> GetStockBajo()
        {
            // Artículos con menos de 5 unidades (puedes parametrizar esto)
            var criticos = await _context.Articulos
                .Where(a => a.Stock < 5)
                .ToListAsync();
            return Ok(criticos);
        }
    }
}