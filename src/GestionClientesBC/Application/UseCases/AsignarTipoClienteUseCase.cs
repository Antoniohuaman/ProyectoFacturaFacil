using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso para asignar o cambiar el TipoCliente de un cliente existente.
    /// </summary>
    public class AsignarTipoClienteUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public AsignarTipoClienteUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        /// <summary>
        /// Asigna un nuevo TipoCliente a un cliente existente.
        /// </summary>
        /// <param name="clienteId">ID del cliente a modificar.</param>
        /// <param name="nuevoTipoCliente">Nuevo tipo de cliente (string, debe ser un valor válido de la enumeración).</param>
        /// <param name="userContext">Contexto del usuario que realiza la operación.</param>
        public async Task AsignarAsync(Guid clienteId, string nuevoTipoCliente, IUserContext userContext)
        {
            // 1. Verificar permisos
            if (!userContext.HasPermission("EditarCliente"))
                throw new UnauthorizedAccessException("No tiene permisos para modificar clientes.");

            // 2. Buscar cliente
            var cliente = await _repo.GetByIdAsync(clienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            // 3. Validar tipo de cliente
            if (!Enum.TryParse<TipoCliente>(nuevoTipoCliente, out var tipoClienteNuevo))
                throw new ValueObjectValidationException($"TipoCliente '{nuevoTipoCliente}' no es válido.");

            // 4a. Si el tipo es igual al actual, no hacer nada
            if (cliente.TipoCliente == tipoClienteNuevo)
                return;

            // 5. Actualizar tipo de cliente
            var tipoAnterior = cliente.TipoCliente;
            cliente.ActualizarTipoCliente(tipoClienteNuevo);

            // 6. Registrar evento de modificación
            var cambios = new Dictionary<string, (object? anterior, object? nuevo)>
            {
                { "TipoCliente", (tipoAnterior, tipoClienteNuevo) }
            };
            cliente.RegistrarModificacion(cambios);

            // 7. Persistir cambios
            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();
        }
    }

    /// <summary>
    /// Excepción para errores de validación de Value Objects.
    /// </summary>
    public class ValueObjectValidationException : Exception
    {
        public ValueObjectValidationException(string message) : base(message) { }
    }
}