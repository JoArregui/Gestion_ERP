using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Services.Transversal;

namespace ERP.Api.Controllers.Transversal
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DivisaController : ControllerBase
    {
        private readonly DivisaService _div;

        public DivisaController(DivisaService div) => _div = div;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        [HttpGet("convertir")]
        public async Task<IActionResult> Convertir([FromQuery] decimal importe, [FromQuery] string desde, [FromQuery] string hacia, [FromQuery] DateTime? fecha = null)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            if (string.IsNullOrWhiteSpace(desde) || string.IsNullOrWhiteSpace(hacia))
                return BadRequest(new { Message = "Divisas requeridas" });
            try
            {
                var resultado = await _div.ConvertirAsync(importe, desde.ToUpper(), hacia.ToUpper(), fecha);
                return Ok(new { ImporteOriginal = importe, Desde = desde.ToUpper(), Hacia = hacia.ToUpper(), Resultado = resultado });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("formatear")]
        public IActionResult Formatear([FromQuery] decimal importe, [FromQuery] string divisa)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            if (string.IsNullOrWhiteSpace(divisa)) return BadRequest(new { Message = "Divisa requerida" });
            return Ok(new { Formateado = _div.Formatear(importe, divisa.ToUpper()) });
        }
    }
}