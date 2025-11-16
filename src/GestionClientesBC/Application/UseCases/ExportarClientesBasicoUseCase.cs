using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.Exportar.Basico
{
	public interface IExportarClientesBasicoUseCase
	{
		Task<ExportarClientesBasicoOutputDto> Handle(CancellationToken ct = default);
	}

	/// <summary>
	/// Genera la plantilla básica de clientes para descarga (cabeceras oficiales + filas).
	/// </summary>
	public sealed class ExportarClientesBasicoUseCase : IExportarClientesBasicoUseCase
	{
		private static readonly string[] CabecerasOficiales =
		{
			"TipoDocumento",
			"NumeroDocumento",
			"RazonSocial",
			"Nombres",
			"Apellidos",
			"NombresCompletos",
			"Correo",
			"Telefonos"
		};

		private readonly IClienteRepository _repo;
		private readonly ITenantContext _tenant;

		public ExportarClientesBasicoUseCase(IClienteRepository repo, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<ExportarClientesBasicoOutputDto> Handle(CancellationToken ct = default)
		{
			var empresaId = _tenant.EmpresaId;
			if (empresaId is null || empresaId.IsEmpty)
				throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

			var clientes = await _repo.GetAllAsync(empresaId, null, null);

			var filas = clientes
				.OrderBy(c => c.RazonSocial?.Valor ?? c.Nombres?.Completo ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.Select(MapearFila)
				.ToList();

			return new ExportarClientesBasicoOutputDto(CabecerasOficiales, filas);
		}

		private static string[] MapearFila(Cliente cliente)
		{
			var nombres = cliente.Nombres;

			return new[]
			{
				cliente.Documento.Tipo.ToString(),
				cliente.Documento.Numero,
				cliente.RazonSocial?.Valor ?? string.Empty,
				nombres?.Nombres ?? string.Empty,
				nombres?.Apellidos ?? string.Empty,
				nombres?.Completo ?? string.Empty,
				cliente.Correo?.Value ?? string.Empty,
				cliente.Telefono?.UnirParaMostrar() ?? string.Empty
			};
		}
	}

	public sealed class ExportarClientesBasicoOutputDto
	{
		public ExportarClientesBasicoOutputDto(IReadOnlyList<string> cabeceras, IReadOnlyList<string[]> filas)
		{
			Cabeceras = cabeceras ?? Array.Empty<string>();
			Filas = filas ?? Array.Empty<string[]>();
		}

		public IReadOnlyList<string> Cabeceras { get; }
		public IReadOnlyList<string[]> Filas { get; }
	}
}
