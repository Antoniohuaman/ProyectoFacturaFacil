using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.Events;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.Eliminar
{
    public interface IEliminarClienteUseCase
    {
        Task<EliminarClienteOutputDto> Handle(EliminarClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Elimina un cliente del sistema. No hay condiciones adicionales para eliminar.
    /// Respeta el aislamiento multi-empresa usando ITenantContext.
    /// Registra el evento de dominio ClienteEliminado para trazabilidad.
    /// </summary>
    public sealed class EliminarClienteUseCase : IEliminarClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EliminarClienteOutputDto> Handle(EliminarClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Obtener empresa actual (aislamiento multitenant)
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 2) Cargar agregado
            var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
            if (cliente is null)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // Validar que el cliente pertenezca a la empresa del tenant
            if (cliente.EmpresaId != empresaId)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 3) Registrar evento de eliminación en el agregado (trazabilidad)
            cliente.EliminarCliente();

            // 4) Eliminar del repositorio
            await _repo.DeleteAsync(empresaId, cliente.ClienteId);

            // 5) Persistencia
            await _uow.SaveChangesAsync(ct);

            // 6) Obtener la fecha del evento (si la infra captura los DomainEvents)
            var fechaEliminacionUtc = cliente.DomainEvents
                .OfType<ClienteEliminado>()
                .LastOrDefault()?.OccurredOn ?? DateTime.UtcNow;

            // 7) Salida
            return new EliminarClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,
                Eliminado = true,
                FechaEliminacionUtc = fechaEliminacionUtc,
                TipoDocumento = cliente.Documento.Tipo.ToString(),
                NumeroDocumento = cliente.Documento.Numero
            };
        }
    }
}
