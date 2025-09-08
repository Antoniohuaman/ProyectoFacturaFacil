using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.EliminarTodos
{
    public interface IEliminarTodosLosClientesUseCase
    {
        Task<EliminarTodosLosClientesOutputDto> Handle(EliminarTodosLosClientesInputDto? input = null, CancellationToken ct = default);
    }

    /// <summary>
    /// Elimina todos los clientes pertenecientes a la empresa (tenant) actual.
    /// No aplica condiciones de negocio adicionales.
    /// </summary>
    public sealed class EliminarTodosLosClientesUseCase : IEliminarTodosLosClientesUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EliminarTodosLosClientesUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EliminarTodosLosClientesOutputDto> Handle(EliminarTodosLosClientesInputDto? input = null, CancellationToken ct = default)
        {
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");


            // 1) Obtener todos los clientes de la empresa actual
            var todos = await _repo.GetAllAsync(empresaId, null, null);
            var afectados = todos.Select(c => c.ClienteId).ToList();

            // 2) Eliminar sólo los de la empresa actual
            if (afectados.Count > 0)
            {
                await _repo.DeleteManyAsync(empresaId, afectados);
                await _uow.SaveChangesAsync(ct);
            }

            // 3) Retorno
            return new EliminarTodosLosClientesOutputDto
            {
                EmpresaId = empresaId.Value,
                Eliminados = afectados.Count,
                FechaEjecucionUtc = DateTime.UtcNow,
                Motivo = input?.Motivo
            };
        }
    }
}
