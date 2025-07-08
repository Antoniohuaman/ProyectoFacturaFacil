using System;

namespace ListaPreciosBC.Domain.Events
{
    public record ListaPrecioInhabilitada(
        Guid ListaPrecioId,
        string UsuarioId,
        string MotivoInhabilitacion,
        DateTime FechaInhabilitacion
    );
}