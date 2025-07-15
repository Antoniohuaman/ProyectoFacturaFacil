using System;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Application.DTOs
{
    public class AdjuntoClienteDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime FechaAdjunto { get; set; }
        public string Observacion { get; set; } = string.Empty;

        public static AdjuntoClienteDto FromEntity(AdjuntoCliente entity)
        {
            return new AdjuntoClienteDto
            {
                Id = entity.AdjuntoId,
                Nombre = entity.NombreArchivo,
                Url = entity.Ruta,
                FechaAdjunto = entity.FechaSubida,
                Observacion = entity.Comentario ?? string.Empty
            };
        }
    }
}