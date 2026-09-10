namespace ERP.Domain.Constants
{
    public static class AppPermissions
    {
        public static readonly List<string> All = new()
        {
            "Seguridad.Usuarios",
            "Seguridad.Roles",
            "Prod.Ver",
            "Prod.Editar",
            "Inv.Stock",
            "Ventas.Facturar"
        };
    }
}