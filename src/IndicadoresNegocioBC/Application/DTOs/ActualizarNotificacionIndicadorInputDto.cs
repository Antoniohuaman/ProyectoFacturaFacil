using System;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class ActualizarNotificacionIndicadorInputDto
    {
        public Guid Id { get; }
        public string? NuevoAsunto { get; }
        public HorarioNotificacion? NuevoHorario { get; }
        public DateTimeOffset? NuevaFechaInicio { get; }
        public DateTimeOffset? NuevaFechaFin { get; }
        public DayOfWeek[]? NuevosDiasSemana { get; }
        public DestinatarioNotificacion? NuevoDestinatario { get; }
        // TODO: Exponer cambio de Medio si se vuelve público en el dominio.

        public ActualizarNotificacionIndicadorInputDto(
            Guid id,
            string? nuevoAsunto = null,
            HorarioNotificacion? nuevoHorario = null,
            DateTimeOffset? nuevaFechaInicio = null,
            DateTimeOffset? nuevaFechaFin = null,
            DayOfWeek[]? nuevosDiasSemana = null,
            DestinatarioNotificacion? nuevoDestinatario = null)
        {
            Id = id;
            NuevoAsunto = nuevoAsunto;
            NuevoHorario = nuevoHorario;
            NuevaFechaInicio = nuevaFechaInicio;
            NuevaFechaFin = nuevaFechaFin;
            NuevosDiasSemana = nuevosDiasSemana;
            NuevoDestinatario = nuevoDestinatario;
        }
    }
}
