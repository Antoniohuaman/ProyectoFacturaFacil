using System;

namespace GestionClientesBC.Domain.Entities
{
    /// <summary>
    /// Medio de contacto adicional de un cliente.
    /// </summary>
    public class ContactoCliente
    {
        public Guid ContactoId { get; }
        public string TipoContacto { get; }
        public string ValorContacto { get; }

        public ContactoCliente(Guid contactoId, string tipoContacto, string valorContacto)
        {
            ContactoId = contactoId != Guid.Empty ? contactoId : throw new ArgumentException("El Id no puede ser vacío.", nameof(contactoId));
            TipoContacto = tipoContacto ?? throw new ArgumentNullException(nameof(tipoContacto));
            ValorContacto = valorContacto ?? throw new ArgumentNullException(nameof(valorContacto));
        }
    }
}