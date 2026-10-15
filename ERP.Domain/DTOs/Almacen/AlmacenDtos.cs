using System.Collections.Generic;

namespace ERP.Domain.DTOs.Almacen
{
    public sealed record HuecoStock(int UbicacionId, string AlmacenCodigo, string UbicacionCodigo, decimal Cantidad, decimal Reservado, decimal Disponible);

    public sealed record StockDetallado(int ArticuloId, string Codigo, string Descripcion, decimal StockTotal, decimal ReservadoTotal, decimal Ubicado, decimal SinUbicar, List<HuecoStock> Huecos);

    public sealed record LineaPicking(int ArticuloId, string Codigo, string Descripcion, decimal CantidadPedida, List<HuecoPicking> Origen);

    public sealed record HuecoPicking(int? UbicacionId, string Origen, decimal Cantidad);

    public sealed record PickingDocumento(int DocumentoId, string NumeroDocumento, List<LineaPicking> Lineas, bool Completo);
}
