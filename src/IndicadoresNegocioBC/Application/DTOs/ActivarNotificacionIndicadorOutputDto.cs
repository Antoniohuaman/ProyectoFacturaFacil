using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class ActivarNotificacionIndicadorOutputDto
    {
        public Guid Id { get; }
        public bool Activo { get; }
        public bool FueIdempotente { get; }
        public ActivarNotificacionIndicadorOutputDto(Guid id, bool activo, bool fueIdempotente)
        {
            Id = id;
            Activo = activo;
            FueIdempotente = fueIdempotente;
        }
    }
}
