using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.Contactos.Eliminar
{
    public interface IEliminarContactoClienteUseCase
    {
        Task<EliminarContactoClienteOutputDto> Handle(EliminarContactoClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Elimina un contacto secundario del cliente en la empresa actual.
    /// </summary>
    public sealed class EliminarContactoClienteUseCase : IEliminarContactoClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarContactoClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EliminarContactoClienteOutputDto> Handle(EliminarContactoClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ClienteId == Guid.Empty)
                throw new BusinessRuleException("ClienteId no puede ser vacío.");
            if (input.ContactoId == Guid.Empty)
                throw new BusinessRuleException("ContactoId no puede ser vacío.");

            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

            var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
            if (cliente is null)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
                throw NotFoundException.For<Cliente>(input.ClienteId);

            var expectedVersion = input.ExpectedVersion ?? cliente.Version;
            cliente.EliminarContacto(input.ContactoId);

            await _repo.UpdateAsync(cliente, expectedVersion);
            await _uow.CommitAsync(ct);

            var evento = cliente.DomainEvents
                .OfType<ContactoEliminado>()
                .LastOrDefault(e => e.ContactoId == input.ContactoId);

            return new EliminarContactoClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,
                ContactoId = input.ContactoId,
                TotalContactos = cliente.Contactos.Count,
                FechaEventoUtc = evento?.OccurredOn ?? cliente.FechaUltimaModificacion,
                Version = cliente.Version
            };
        }
    }
}
