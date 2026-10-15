using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Services.Transversal;

namespace ERP.Api.Controllers.Transversal
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LocalizacionController : ControllerBase
    {
        private readonly LocalizacionService _loc;

        public LocalizacionController(LocalizacionService loc) => _loc = loc;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        [HttpGet("culturas")]
        public async Task<IActionResult> GetCulturas()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _loc.GetCulturasAsync(GetEmpresaId()));
        }

        [HttpGet("traducir")]
        public IActionResult Traducir([FromQuery] string clave, [FromQuery] string? cultura = null)
        {
            if (string.IsNullOrWhiteSpace(clave)) return BadRequest(new { Message = "Clave requerida" });
            var c = string.IsNullOrWhiteSpace(cultura) ? null : System.Globalization.CultureInfo.GetCultureInfo(cultura);
            return Ok(new { Clave = clave, Valor = _loc.T(clave, c) });
        }
    }
}