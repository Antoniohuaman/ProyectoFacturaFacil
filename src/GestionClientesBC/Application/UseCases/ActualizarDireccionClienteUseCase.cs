using System;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Direccion.Actualizar
{
	public interface IActualizarDireccionClienteUseCase
	{
		Task<ActualizarDireccionClienteOutputDto> Handle(ActualizarDireccionClienteInputDto input, CancellationToken ct = default);
	}

	/// <summary>
	/// Actualiza el domicilio fiscal del cliente dentro de la empresa actual.
	/// </summary>
	public sealed class ActualizarDireccionClienteUseCase : IActualizarDireccionClienteUseCase
	{
		private readonly IClienteRepository _repo;
		private readonly IUnitOfWork _uow;
		private readonly ITenantContext _tenant;

		public ActualizarDireccionClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<ActualizarDireccionClienteOutputDto> Handle(ActualizarDireccionClienteInputDto input, CancellationToken ct = default)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));
			if (input.ClienteId == Guid.Empty)
				throw new BusinessRuleException("ClienteId no puede ser vacío.");

			var empresaId = _tenant.EmpresaId;
			if (empresaId is null || empresaId.IsEmpty)
				throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

			var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
			if (cliente is null)
				throw NotFoundException.For<Cliente>(input.ClienteId);

			if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
				throw NotFoundException.For<Cliente>(input.ClienteId);

			bool hayDataDireccion =
				!string.IsNullOrWhiteSpace(input.DireccionLinea) ||
				!string.IsNullOrWhiteSpace(input.Ubigeo) ||
				!string.IsNullOrWhiteSpace(input.Departamento) ||
				!string.IsNullOrWhiteSpace(input.Provincia) ||
				!string.IsNullOrWhiteSpace(input.Distrito) ||
				!string.IsNullOrWhiteSpace(input.AddressTypeCode);

			if (!hayDataDireccion)
				throw new BusinessRuleException("Debe proporcionar al menos un dato de dirección.");

			var pais = string.IsNullOrWhiteSpace(input.PaisCodigoIso)
				? "PE"
				: input.PaisCodigoIso!.Trim().ToUpperInvariant();

			string? linea = Normalize(input.DireccionLinea);
			string? ubigeo = Normalize(input.Ubigeo);
			string? departamento = Normalize(input.Departamento);
			string? provincia = Normalize(input.Provincia);
			string? distrito = Normalize(input.Distrito);
			string? addressType = Normalize(input.AddressTypeCode);

			var domicilio = string.Equals(pais, "PE", StringComparison.OrdinalIgnoreCase)
				? DomicilioFiscal.FromPeru(linea, ubigeo, departamento, provincia, distrito, addressType)
				: DomicilioFiscal.From(pais, linea, ubigeo, departamento, provincia, distrito, addressType);

			var expectedVersion = input.ExpectedVersion ?? cliente.Version;
			cliente.ActualizarDireccion(domicilio);

			await _repo.UpdateAsync(cliente, expectedVersion);
			await _uow.CommitAsync(ct);

			var fechaActualizacion = cliente.FechaUltimaModificacion ?? DateTime.UtcNow;

			return new ActualizarDireccionClienteOutputDto
			{
				ClienteId = cliente.ClienteId,
				EmpresaId = empresaId.Value,
				PaisCodigoIso = domicilio.PaisCodigoIso,
				DireccionLinea = domicilio.Linea,
				Ubigeo = domicilio.Ubigeo,
				Departamento = domicilio.Departamento,
				Provincia = domicilio.Provincia,
				Distrito = domicilio.Distrito,
				AddressTypeCode = domicilio.AddressTypeCode,
				DireccionFormateada = domicilio.ToString(),
				FechaActualizacionUtc = fechaActualizacion,
				Version = cliente.Version
			};
		}

		private static string? Normalize(string? value)
			=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}
}
