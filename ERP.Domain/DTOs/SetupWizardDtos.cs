using System;

namespace ERP.Domain.Dtos
{
    public class SetupStatusDto
    {
        public int EmpresaId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int Familias { get; set; }
        public int Articulos { get; set; }
        public int Proveedores { get; set; }
        public int Clientes { get; set; }
        public int Empleados { get; set; }
        public bool Dismissed { get; set; }
        public bool Completado { get; set; }
        /// <summary>Debe mostrarse el wizard este login (no bootstrap, con empresa, no visto/completado)</summary>
        public bool DebeMostrar { get; set; }
        public int PasosObligatoriosCompletados { get; set; }
        public int PasosObligatoriosTotales { get; set; } = 2; // Familias + Artículos
        public double ProgresoObligatorio => PasosObligatoriosTotales == 0 ? 1 : (double)PasosObligatoriosCompletados / PasosObligatoriosTotales;
    }

    public class SetupActionResultDto
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
