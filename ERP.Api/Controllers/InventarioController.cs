using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using System.Security.Claims;

namespace ERP.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InventarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;
        private bool IsGeneric => string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.local", System.StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst(ClaimTypes.Email)?.Value, "admin@erp.com", System.StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.local", System.StringComparison.OrdinalIgnoreCase)
                               || string.Equals(User.FindFirst("email")?.Value, "admin@erp.com", System.StringComparison.OrdinalIgnoreCase);

        [HttpGet("stock-bajo")]
        public async Task<IActionResult> GetStockBajo()
        {
            if (IsGeneric) return Ok(new List<ERP.Domain.Entities.Articulo>());
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return Ok(new List<ERP.Domain.Entities.Articulo>());
            var criticos = await _context.Articulos
                .Where(a => a.EmpresaId == empresaId && a.Stock < 5)
                .ToListAsync();
            return Ok(criticos);
        }
    }
}