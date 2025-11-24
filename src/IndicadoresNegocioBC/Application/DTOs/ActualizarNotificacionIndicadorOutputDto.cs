using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class ActualizarNotificacionIndicadorOutputDto
    {
        public Guid Id { get; }
        public bool HuboCambios { get; }
        public bool Activo { get; }
        public string Asunto { get; }
        public string Horario { get; }
        public DateTimeOffset? FechaInicio { get; }
        public DateTimeOffset? FechaFin { get; }
        public DayOfWeek[]? DiasSemana { get; }
        public string Medio { get; }
        public string Destinatario { get; }

        public ActualizarNotificacionIndicadorOutputDto(
            Guid id,
            bool huboCambios,
            bool activo,
            string asunto,
            string horario,
            DateTimeOffset? fechaInicio,
            DateTimeOffset? fechaFin,
            DayOfWeek[]? diasSemana,
            string medio,
            string destinatario)
        {
            Id = id;
            HuboCambios = huboCambios;
            Activo = activo;
            Asunto = asunto;
            Horario = horario;
            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
            DiasSemana = diasSemana;
            Medio = medio;
            Destinatario = destinatario;
        }
    }
}
