using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class EliminarNotificacionIndicadorInputDto
    {
        public Guid Id { get; }
        public EliminarNotificacionIndicadorInputDto(Guid id) => Id = id;
    }
}
