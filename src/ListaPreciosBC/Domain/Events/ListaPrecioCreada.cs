using System;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;

namespace ListaPreciosBC.Domain.Events
{
    public record ListaPrecioCreada(
        Guid ListaPrecioId,
        TipoLista TipoLista,
        CriterioLista CriterioLista,
        string UsuarioId,
        DateTime FechaCreacion
    );
}