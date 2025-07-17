using System;
using System.Collections.Generic;

namespace ListaPreciosBC.Domain.Events
{
    public record PrecioEspecificoModificado(
        Guid PrecioEspecificoId,
        List<(string Campo, object? Anterior, object? Nuevo)> CamposModificados,
        string UsuarioId,
        DateTime FechaModificacion,
        string Motivo
    );
}