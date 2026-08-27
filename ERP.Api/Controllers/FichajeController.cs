using Microsoft.AspNetCore.Mvc;
using ERP.Services;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FichajeController : ControllerBase
    {
        private readonly RRHHService _rrhhService;
        private readonly ApplicationDbContext _context;

        public FichajeController(RRHHService rrhhService, ApplicationDbContext context)
        {
            _rrhhService = rrhhService;
            _context = context;
        }

        [HttpGet("historial")]
        public async Task<ActionResult<IEnumerable<object>>> GetHistorial([FromQuery] int? empleadoId)
        {
            var q = _context.ControlesHorarios.Include(c => c.Empleado).AsQueryable();
            if (empleadoId.HasValue) q = q.Where(c => c.EmpleadoId == empleadoId.Value);
            var lista = await q.OrderByDescending(c => c.Entrada).Take(50)
                .Select(c => new {
                    c.Id,
                    c.EmpleadoId,
                    EmpleadoNombre = c.Empleado != null ? c.Empleado.Nombre + " " + c.Empleado.Apellidos : "",
                    c.Entrada,
                    c.Salida,
                    TotalHoras = c.Salida.HasValue ? (c.Salida.Value - c.Entrada).TotalHours : (double?)null
                }).ToListAsync();
            return Ok(lista);
        }

        [HttpGet("estado/{empleadoId}")]
        public async Task<ActionResult<object>> GetEstado(int empleadoId)
        {
            var activo = await _context.ControlesHorarios.AnyAsync(c => c.EmpleadoId == empleadoId && c.Salida == null);
            return Ok(new { fichado = activo });
        }

        /// <summary>
        /// Registra la entrada validando el PIN del empleado
        /// </summary>
        [HttpPost("entrada")]
        public async Task<IActionResult> Entrada([FromBody] FichajeRequest request)
        {
            try 
            {
                var resultado = await _rrhhService.RegistrarEntradaConPin(request.EmpleadoId, request.Pin);
                
                if (resultado)
                {
                    return Ok(new { 
                        mensaje = "Entrada registrada correctamente",
                        hora = DateTime.Now.ToString("HH:mm:ss")
                    });
                }
                
                return Unauthorized("El PIN introducido es incorrecto.");
            }
            catch (Exception) // Se elimina 'ex' para evitar CS0168 si no se va a usar
            {
                return BadRequest("Ocurrió un error inesperado al procesar la entrada.");
            }
        }

        /// <summary>
        /// Registra la salida validando el PIN del empleado
        /// </summary>
        [HttpPost("salida")]
        public async Task<IActionResult> Salida([FromBody] FichajeRequest request)
        {
            try 
            {
                var resultado = await _rrhhService.RegistrarSalidaConPin(request.EmpleadoId, request.Pin);
                
                if (resultado)
                {
                    return Ok(new { 
                        mensaje = "Salida registrada correctamente",
                        hora = DateTime.Now.ToString("HH:mm:ss")
                    });
                }
                
                return Unauthorized("El PIN introducido es incorrecto.");
            }
            catch (Exception) // Se elimina 'ex' para evitar CS0168
            {
                return BadRequest("Ocurrió un error inesperado al procesar la salida.");
            }
        }
    }

    public class FichajeRequest
    {
        public required int EmpleadoId { get; set; }
        public required string Pin { get; set; } = string.Empty;
    }
}