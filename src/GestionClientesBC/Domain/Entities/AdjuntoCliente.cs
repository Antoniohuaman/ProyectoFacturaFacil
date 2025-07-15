using System;

namespace GestionClientesBC.Domain.Entities
{
    public class AdjuntoCliente
    {
        public Guid AdjuntoId { get; private set; }
        public string NombreArchivo { get; private set; }
        public string Ruta { get; private set; }
        public DateTime FechaSubida { get; private set; }
        public string? Comentario { get; private set; }
        public bool Activo { get; private set; }

        public AdjuntoCliente(Guid adjuntoId, string nombreArchivo, string ruta, DateTime fechaSubida, string? comentario)
        {
            AdjuntoId = adjuntoId;
            NombreArchivo = nombreArchivo;
            Ruta = ruta;
            FechaSubida = fechaSubida;
            Comentario = comentario;
            Activo = true;
        }

        public void MarcarInactivo()
        {
            Activo = false;
        }
    }
}