using System;

namespace ListaPreciosBC.Domain.Entities
{
    public class HistorialPrecio
    {
        public Guid HistorialPrecioId { get; private set; }
        public Guid PrecioEspecificoId { get; private set; }
        public string UsuarioId { get; private set; }
        public DateTime FechaCambio { get; private set; }
        public string Motivo { get; private set; }
        public decimal ValorAnterior { get; private set; }
        public decimal ValorNuevo { get; private set; }

        public HistorialPrecio(
            Guid historialPrecioId,
            Guid precioEspecificoId,
            string usuarioId,
            DateTime fechaCambio,
            string motivo,
            decimal valorAnterior,
            decimal valorNuevo)
        {
            HistorialPrecioId = historialPrecioId;
            PrecioEspecificoId = precioEspecificoId;
            UsuarioId = usuarioId;
            FechaCambio = fechaCambio;
            Motivo = motivo;
            ValorAnterior = valorAnterior;
            ValorNuevo = valorNuevo;
        }
    }
}