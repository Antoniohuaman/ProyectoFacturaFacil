using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso para gestionar contactos secundarios de un cliente (agregar, editar, eliminar).
    /// </summary>
    public class GestionarContactoUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public GestionarContactoUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        /// <summary>
        /// Agrega un contacto secundario a un cliente.
        /// </summary>
        public async Task<Guid> AgregarContactoAsync(Guid clienteId, TipoContacto tipo, string valor, IUserContext userContext)
        {
            // 1. Permisos
            if (!userContext.HasPermission("EditarCliente"))
                throw new UnauthorizedAccessException("No tiene permisos para editar clientes.");

            // 2. Buscar cliente
            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            // 3. Validar formato
            ValidarFormato(tipo, valor);

            // 4. Validar duplicado (opcional, según política)
            if (cliente.Contactos.Any(c => c.Tipo == tipo && c.Valor.Equals(valor, StringComparison.OrdinalIgnoreCase)))
                throw new ContactoDuplicadoException("Ya existe un contacto igual para este cliente.");

            // 5. Crear y agregar contacto
            var contacto = new ContactoCliente(Guid.NewGuid(), tipo, valor);
            cliente.AgregarContacto(contacto);

            // 6. Publicar evento
            cliente.RegistrarEvento(new ContactoAgregado(clienteId, contacto.ContactoId, tipo, valor));

            // 7. Persistir
            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();

            return contacto.ContactoId;
        }

        /// <summary>
        /// Edita un contacto existente de un cliente.
        /// </summary>
        public async Task EditarContactoAsync(Guid clienteId, Guid contactoId, string nuevoValor, IUserContext userContext)
        {
            if (!userContext.HasPermission("EditarCliente"))
                throw new UnauthorizedAccessException("No tiene permisos para editar clientes.");

            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            var contacto = cliente.Contactos.FirstOrDefault(c => c.ContactoId == contactoId);
            if (contacto == null)
                throw new EntityNotFoundException("Contacto no encontrado.");

            ValidarFormato(contacto.Tipo, nuevoValor);

            contacto.ActualizarValor(nuevoValor);

            cliente.RegistrarEvento(new ClienteModificado(clienteId, DateTime.UtcNow));

            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();
        }

        /// <summary>
        /// Elimina un contacto existente de un cliente.
        /// </summary>
        public async Task EliminarContactoAsync(Guid clienteId, Guid contactoId, IUserContext userContext)
        {
            if (!userContext.HasPermission("EditarCliente"))
                throw new UnauthorizedAccessException("No tiene permisos para editar clientes.");

            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            var contacto = cliente.Contactos.FirstOrDefault(c => c.ContactoId == contactoId);
            if (contacto == null)
                throw new EntityNotFoundException("Contacto no encontrado.");

            cliente.EliminarContacto(contactoId);

            cliente.RegistrarEvento(new ClienteModificado(clienteId, DateTime.UtcNow));

            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();
        }

        /// <summary>
        /// Valida el formato del valor de contacto según el tipo.
        /// </summary>
        private void ValidarFormato(TipoContacto tipo, string valor)
        {
            switch (tipo)
            {
                case TipoContacto.EMAIL_SECUNDARIO:
                    if (string.IsNullOrWhiteSpace(valor) || !Regex.IsMatch(valor, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        throw new ContactoInvalidoException("Formato de email inválido.");
                    break;
                case TipoContacto.TELEFONO_SECUNDARIO:
                    if (string.IsNullOrWhiteSpace(valor) || !Regex.IsMatch(valor, @"^\d{7,15}$"))
                        throw new ContactoInvalidoException("Formato de teléfono inválido.");
                    break;
                case TipoContacto.DIRECCION:
                    if (string.IsNullOrWhiteSpace(valor) || valor.Length > 200)
                        throw new ContactoInvalidoException("Dirección inválida o demasiado larga.");
                    break;
                default:
                    throw new ContactoInvalidoException("Tipo de contacto no soportado.");
            }
        }
    }

    // Excepciones específicas
    public class ContactoInvalidoException : Exception
    {
        public ContactoInvalidoException(string message) : base(message) { }
    }

    public class ContactoDuplicadoException : Exception
    {
        public ContactoDuplicadoException(string message) : base(message) { }
    }

    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string message) : base(message) { }
    }

    // Eventos de dominio
    public class ContactoAgregado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public Guid ContactoId { get; }
        public TipoContacto Tipo { get; }
        public string Valor { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ContactoAgregado(Guid clienteId, Guid contactoId, TipoContacto tipo, string valor)
        {
            ClienteId = clienteId;
            ContactoId = contactoId;
            Tipo = tipo;
            Valor = valor;
        }
    }

    public class ClienteModificado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public DateTime FechaModificacion { get; }
        public DateTime OccurredOn => FechaModificacion;

        public ClienteModificado(Guid clienteId, DateTime fechaModificacion)
        {
            ClienteId = clienteId;
            FechaModificacion = fechaModificacion;
        }
    }
}