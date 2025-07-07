using System;

namespace ControlCajaBC.Domain.Entities
{
    /// <summary>
    /// Identifica a la persona responsable de la apertura/cierre de caja.
    /// </summary>
    public sealed class ResponsableCaja
    {
        public Guid Id { get; }
        public string Nombre { get; }

        public ResponsableCaja(Guid id, string nombre)
        {
            Id = id != Guid.Empty ? id : throw new ArgumentException("El Id no puede ser vacío.", nameof(id));
            Nombre = !string.IsNullOrWhiteSpace(nombre) 
                ? nombre 
                : throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));
        }
    }
}
