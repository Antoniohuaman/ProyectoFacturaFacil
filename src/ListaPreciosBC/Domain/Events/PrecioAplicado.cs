using System;

namespace ListaPreciosBC.Domain.Events
{
    public record PrecioAplicado(
        Guid ListaPrecioId,
        Guid PrecioEspecificoId,
        Guid ComprobanteId,
        decimal Monto,
        DateTime FechaAplicacion
    );
}