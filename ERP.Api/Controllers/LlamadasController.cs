using Microsoft.AspNetCore.Mvc;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LlamadasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LlamadasController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la lista de llamadas/comunicaciones registradas en el sistema
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LlamadaDTO>>> GetLlamadas()
        {
            var registros = await _context.ControlesHorarios
                .Where(c => c.Salida.HasValue)  // Salida es nullable, Entrada siempre tiene valor
                .OrderByDescending(c => c.Entrada)
                .Take(20)
                .ToListAsync();

            var llamadas = registros.Select(c => new LlamadaDTO
            {
                Empresa = c.Empleado?.Empresa?.NombreComercial ?? "Empresa Desconocida",
                Motivo = $"Fichaje: " + c.Entrada.ToString("HH:mm"),
                Telefono = c.Empleado?.DNI ?? "N/A",
                Tiempo = (c.Salida.Value - c.Entrada).ToString(@"hh\:mm"),
                Urgente = (c.Salida.Value - c.Entrada).TotalHours > 8
            }).ToList();

            return Ok(llamadas);
        }
    }

    public class LlamadaDTO
    {
        public string Empresa { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Tiempo { get; set; } = string.Empty;
        public bool Urgente { get; set; }
    }
}