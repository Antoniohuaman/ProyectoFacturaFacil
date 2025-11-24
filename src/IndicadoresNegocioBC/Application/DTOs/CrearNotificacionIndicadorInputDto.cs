using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class CrearNotificacionIndicadorInputDto
    {
        public Guid IndicadorId { get; }
        public EstablecimientoId EstablecimientoId { get; }
        public UsuarioId UsuarioId { get; }
        public string Asunto { get; }
        public HorarioNotificacion HorarioEnvio { get; }
        public MedioNotificacion Medio { get; }
        public DestinatarioNotificacion Destinatario { get; }
        public bool ActivoInicial { get; }

        public CrearNotificacionIndicadorInputDto(
            Guid indicadorId,
            EstablecimientoId establecimientoId,
            UsuarioId usuarioId,
            string asunto,
            HorarioNotificacion horarioEnvio,
            MedioNotificacion medio,
            DestinatarioNotificacion destinatario,
            bool activoInicial = true)
        {
            IndicadorId = indicadorId;
            EstablecimientoId = establecimientoId;
            UsuarioId = usuarioId;
            Asunto = asunto;
            HorarioEnvio = horarioEnvio;
            Medio = medio;
            Destinatario = destinatario;
            ActivoInicial = activoInicial;
        }
    }
}
