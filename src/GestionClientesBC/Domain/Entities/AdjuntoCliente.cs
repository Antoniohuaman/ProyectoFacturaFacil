using System;

namespace GestionClientesBC.Domain.Entities
{
    /// <summary>
    /// Documento o imagen asociado a un cliente.
    /// </summary>
    public class AdjuntoCliente
    {
        public Guid AdjuntoId { get; }
        public string TipoAdjunto { get; }
        public string NombreArchivo { get; }
        public string Ruta { get; }
        public DateTime FechaSubida { get; }

        public AdjuntoCliente(Guid adjuntoId, string tipoAdjunto, string nombreArchivo, string ruta, DateTime fechaSubida)
        {
            AdjuntoId = adjuntoId != Guid.Empty ? adjuntoId : throw new ArgumentException("El Id no puede ser vacío.", nameof(adjuntoId));
            TipoAdjunto = tipoAdjunto ?? throw new ArgumentNullException(nameof(tipoAdjunto));
            NombreArchivo = nombreArchivo ?? throw new ArgumentNullException(nameof(nombreArchivo));
            Ruta = ruta ?? throw new ArgumentNullException(nameof(ruta));
            FechaSubida = fechaSubida;
        }
    }
}