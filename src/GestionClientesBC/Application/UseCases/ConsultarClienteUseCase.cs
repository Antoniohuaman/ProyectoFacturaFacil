using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.Consultar
{
    public interface IConsultarClienteUseCase
    {
        Task<ConsultarClienteOutputDto> Handle(ConsultarClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Consulta un cliente por Id o Documento dentro del tenant/empresa actual.
    /// </summary>
    public sealed class ConsultarClienteUseCase : IConsultarClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly ITenantContext _tenant;

        public ConsultarClienteUseCase(IClienteRepository repo, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ConsultarClienteOutputDto> Handle(ConsultarClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

            Cliente? cliente = null;

            // 1) Resolver por Id o por documento
            if (input.ClienteId.HasValue && input.ClienteId != Guid.Empty)
            {
                cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId.Value);
            }
            else if (input.TipoDocumento.HasValue && !string.IsNullOrWhiteSpace(input.NumeroDocumento))
            {
                // SearchAsync filtra por 'filtro' en varios campos; luego ajustamos por Empresa/Tipo/Numero
                var posibles = await _repo.SearchAsync(empresaId, input.NumeroDocumento!.Trim(), null, null);
                cliente = posibles.FirstOrDefault(c =>
                    c.EmpresaId.EsMismaEmpresaQue(empresaId) &&
                    c.Documento.Tipo == input.TipoDocumento!.Value &&
                    string.Equals(c.Documento.Numero, input.NumeroDocumento!.Trim(), StringComparison.Ordinal));
            }
            else
            {
                throw new BusinessRuleException("Debe proporcionar ClienteId o (TipoDocumento y NumeroDocumento).");
            }

            // 2) Validar existencia y pertenencia a empresa
            if (cliente is null || !cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
                throw NotFoundException.For<Cliente>(input.ClienteId?.ToString() ?? input.NumeroDocumento);

            // 3) Mapear a salida
            var dto = ConsultarClienteOutputDto.From(cliente, incluirContactos: input.IncluirContactos, incluirAdjuntos: input.IncluirAdjuntos);
            dto.EmpresaId = empresaId.Value;
            return dto;
        }
    }
}
