using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class ActivarNotificacionIndicadorInputDto
    {
        public Guid Id { get; }
        public ActivarNotificacionIndicadorInputDto(Guid id) => Id = id;
    }
}
