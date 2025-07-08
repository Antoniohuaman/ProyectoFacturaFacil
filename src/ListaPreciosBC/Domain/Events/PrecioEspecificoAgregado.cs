using System;
using ListaPreciosBC.Domain.Entities;

namespace ListaPreciosBC.Domain.Events
{
    public record PrecioEspecificoAgregado(
        Guid PrecioEspecificoId,
        Guid ListaPrecioId,
        decimal Valor,
        CriterioLista CriterioPrecio,
        string UsuarioId,
        DateTime FechaCreacion
    );
}