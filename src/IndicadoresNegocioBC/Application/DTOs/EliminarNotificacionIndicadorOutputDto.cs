using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class EliminarNotificacionIndicadorOutputDto
    {
        public Guid Id { get; }
        public bool Eliminado { get; }
        public EliminarNotificacionIndicadorOutputDto(Guid id, bool eliminado)
        {
            Id = id;
            Eliminado = eliminado;
        }
    }
}
