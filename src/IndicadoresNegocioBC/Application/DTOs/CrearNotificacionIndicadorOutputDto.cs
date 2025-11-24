using System;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed class CrearNotificacionIndicadorOutputDto
    {
        public Guid Id { get; }
        public Guid IndicadorId { get; }
        public EmpresaId EmpresaId { get; }
        public EstablecimientoId EstablecimientoId { get; }
        public UsuarioId UsuarioId { get; }
        public string Asunto { get; }
        public string Horario { get; }
        public string Medio { get; }
        public string Destinatario { get; }
        public bool Activo { get; }
        public DateTimeOffset FechaCreacion { get; }

        public CrearNotificacionIndicadorOutputDto(
            Guid id,
            Guid indicadorId,
            EmpresaId empresaId,
            EstablecimientoId establecimientoId,
            UsuarioId usuarioId,
            string asunto,
            string horario,
            string medio,
            string destinatario,
            bool activo,
            DateTimeOffset fechaCreacion)
        {
            Id = id;
            IndicadorId = indicadorId;
            EmpresaId = empresaId;
            EstablecimientoId = establecimientoId;
            UsuarioId = usuarioId;
            Asunto = asunto;
            Horario = horario;
            Medio = medio;
            Destinatario = destinatario;
            Activo = activo;
            FechaCreacion = fechaCreacion;
        }
    }
}
