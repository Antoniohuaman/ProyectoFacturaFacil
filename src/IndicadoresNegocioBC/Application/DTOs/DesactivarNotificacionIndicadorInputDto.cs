using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class DesactivarNotificacionIndicadorInputDto
    {
        public Guid Id { get; }
        public DesactivarNotificacionIndicadorInputDto(Guid id) => Id = id;
    }
}
