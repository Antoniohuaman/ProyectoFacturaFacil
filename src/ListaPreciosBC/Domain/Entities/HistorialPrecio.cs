using System;
using System.Collections.Generic;

namespace ListaPreciosBC.Domain.Entities
{
    public class HistorialPrecio
    {
        public Guid HistorialPrecioId { get; private set; }
        public Guid PrecioEspecificoId { get; private set; }
        public string UsuarioId { get; private set; }
        public DateTime FechaCambio { get; private set; }
        public string Motivo { get; private set; }
        public List<(string Campo, object? Anterior, object? Nuevo)> Cambios { get; private set; }

        public HistorialPrecio(
            Guid historialPrecioId,
            Guid precioEspecificoId,
            string usuarioId,
            DateTime fechaCambio,
            List<(string Campo, object? Anterior, object? Nuevo)> cambios,
            string motivo)
        {
            HistorialPrecioId = historialPrecioId;
            PrecioEspecificoId = precioEspecificoId;
            UsuarioId = usuarioId;
            FechaCambio = fechaCambio;
            Cambios = cambios;
            Motivo = motivo;
        }
    }
}