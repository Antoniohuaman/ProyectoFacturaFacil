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
    /// Caso de uso para deshabilitar (inactivar) un cliente.
    /// </summary>
    public class DeshabilitarClienteUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public DeshabilitarClienteUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        /// <summary>
        /// Deshabilita un cliente, cambiando su estado a INACTIVO, registrando motivo y fecha.
        /// </summary>
        public async Task DeshabilitarAsync(Guid clienteId, string? motivo, IUserContext userContext)
        {
            // 1. Verificar permisos
            if (!userContext.HasPermission("DeshabilitarCliente"))
                throw new UnauthorizedAccessException("No tiene permisos para deshabilitar clientes.");

            // 2. Buscar cliente
            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            // 2a. Cliente ya inactivo
            if (cliente.Estado != EstadoCliente.Activo)
                throw new InvalidOperationException("El cliente ya está inactivo.");

            // 3. Verificar operaciones pendientes
            if (TieneOperacionesPendientes(cliente))
                throw new ClienteConCuentasPendientesException("Cliente con cuentas pendientes, no se puede deshabilitar.");

            // 4. Cambiar estado y registrar motivo/fecha
            cliente.Deshabilitar(motivo, DateTime.UtcNow);

            // 5. Registrar evento de deshabilitación
            cliente.RegistrarDeshabilitacion(motivo, DateTime.UtcNow);

            // 6. Persistir cambios
            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();
        }

        /// <summary>
        /// Verifica si el cliente tiene operaciones pendientes.
        /// </summary>
        private bool TieneOperacionesPendientes(Cliente cliente)
        {
            // Revisa si hay operaciones con estado pendiente.
            // Asegúrate que OperacionCliente tenga la propiedad EstaPendiente.
            return cliente.Operaciones.Any(op => op.EstaPendiente);
        }
    }

    /// <summary>
    /// Excepción para clientes con cuentas pendientes.
    /// </summary>
    public class ClienteConCuentasPendientesException : Exception
    {
        public ClienteConCuentasPendientesException(string message) : base(message) { }
    }
}