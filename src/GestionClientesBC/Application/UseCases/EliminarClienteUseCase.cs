using System;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Events;

namespace GestionClientesBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso para eliminar lógicamente un cliente del sistema.
    /// </summary>
    public class EliminarClienteUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public EliminarClienteUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        /// <summary>
        /// Elimina lógicamente un cliente, verificando políticas de retención y operaciones asociadas.
        /// </summary>
        /// <param name="clienteId">ID del cliente a eliminar.</param>
        /// <param name="userContext">Contexto del usuario que realiza la operación.</param>
        public async Task EliminarAsync(Guid clienteId, IUserContext userContext)
        {
            // 1. Verificar permisos especiales
            if (!userContext.HasPermission("EliminarCliente"))
                throw new UnauthorizedAccessException("No tiene permisos para eliminar clientes.");

            // 2. Buscar cliente
            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            // 3. Verificar estado permitido
            if (cliente.Estado != EstadoCliente.Activo && cliente.Estado != EstadoCliente.Inactivo)
                throw new InvalidOperationException("Solo se pueden eliminar clientes activos o inactivos.");

            // 4. Verificar que no tenga operaciones activas o registros que impidan eliminar
            if (TieneOperacionesQueImpidenEliminacion(cliente))
                throw new ClienteConOperacionesRegistradasException("No se puede eliminar cliente con operaciones registradas.");

            // 5. Eliminar lógicamente el cliente (puedes marcarlo como eliminado o removerlo del repositorio)
            await _repo.DeleteAsync(cliente.ClienteId);

            // 6. Publicar evento de eliminación
            var evento = new ClienteEliminado(cliente.ClienteId, DateTime.UtcNow);
            // Si tienes un bus de eventos, publícalo aquí. Si no, puedes agregarlo a los eventos de dominio:
            // cliente.RegistrarEliminacion(DateTime.UtcNow);
            // Pero como el cliente ya no existe, lo ideal es publicarlo aquí o en el repositorio.

            // 7. Confirmar cambios
            await _uow.CommitAsync();
        }

        /// <summary>
        /// Verifica si el cliente tiene operaciones que impiden su eliminación.
        /// </summary>
        private bool TieneOperacionesQueImpidenEliminacion(Cliente cliente)
        {
            // Por ejemplo, si tiene operaciones activas o registros que deben conservarse.
            // Puedes adaptar esta lógica según tus políticas de retención.
            return cliente.Operaciones.Any(op => op.EstaPendiente || op.EsCriticaParaRetencion);
        }
    }

    /// <summary>
    /// Excepción para clientes con operaciones que impiden su eliminación.
    /// </summary>
    public class ClienteConOperacionesRegistradasException : Exception
    {
        public ClienteConOperacionesRegistradasException(string message) : base(message) { }
    }

    /// <summary>
    /// Evento de dominio para cliente eliminado.
    /// </summary>
    public class ClienteEliminado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public DateTime FechaEliminacion { get; }
        public DateTime OccurredOn => FechaEliminacion;

        public ClienteEliminado(Guid clienteId, DateTime fechaEliminacion)
        {
            ClienteId = clienteId;
            FechaEliminacion = fechaEliminacion;
        }
    }
}