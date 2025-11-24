using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class DesactivarNotificacionIndicadorOutputDto
    {
        public Guid Id { get; }
        public bool Activo { get; }
        public bool FueIdempotente { get; }
        public DesactivarNotificacionIndicadorOutputDto(Guid id, bool activo, bool fueIdempotente)
        {
            Id = id;
            Activo = activo;
            FueIdempotente = fueIdempotente;
        }
    }
}
