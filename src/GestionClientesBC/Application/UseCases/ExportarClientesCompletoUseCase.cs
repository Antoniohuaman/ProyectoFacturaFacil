using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.Exportar.Completo
{
	public interface IExportarClientesCompletoUseCase
	{
		Task<ExportarClientesCompletoOutputDto> Handle(CancellationToken ct = default);
	}

	/// <summary>
	/// Exporta la plantilla completa (todos los campos soportados por la importación completa).
	/// </summary>
	public sealed class ExportarClientesCompletoUseCase : IExportarClientesCompletoUseCase
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
			"Telefonos",
			"NombreComercial",
			"PaginaWeb",
			"Observaciones",
			"PaisCodigoIso",
			"DireccionLinea",
			"Ubigeo",
			"Departamento",
			"Provincia",
			"Distrito",
			"AddressTypeCode",
			"TipoClienteCodigo",
			"RolClienteCodigo",
			"FotoPerfilNombreArchivo",
			"FotoPerfilUrl"
		};

		private readonly IClienteRepository _repo;
		private readonly ITenantContext _tenant;

		public ExportarClientesCompletoUseCase(IClienteRepository repo, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<ExportarClientesCompletoOutputDto> Handle(CancellationToken ct = default)
		{
			var empresaId = _tenant.EmpresaId;
			if (empresaId is null || empresaId.IsEmpty)
				throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

			var clientes = await _repo.GetAllAsync(empresaId, null, null);

			var filas = clientes
				.OrderBy(c => c.RazonSocial?.Valor ?? c.Nombres?.Completo ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.Select(MapearFila)
				.ToList();

			return new ExportarClientesCompletoOutputDto(CabecerasOficiales, filas);
		}

		private static string[] MapearFila(Cliente cliente)
		{
			var nombres = cliente.Nombres;
			var domicilio = cliente.DomicilioFiscal;
			var foto = cliente.FotoPerfil;

			return new[]
			{
				cliente.Documento.Tipo.ToString(),
				cliente.Documento.Numero,
				cliente.RazonSocial?.Valor ?? string.Empty,
				nombres?.Nombres ?? string.Empty,
				nombres?.Apellidos ?? string.Empty,
				nombres?.Completo ?? string.Empty,
				cliente.Correo?.Value ?? string.Empty,
				cliente.Telefono?.UnirParaMostrar() ?? string.Empty,
				cliente.NombreComercial?.ParaMostrar ?? string.Empty,
				cliente.PaginaWeb?.Valor ?? string.Empty,
				cliente.Observaciones?.Valor ?? string.Empty,
				domicilio?.PaisCodigoIso ?? "PE",
				domicilio?.Linea ?? string.Empty,
				domicilio?.Ubigeo ?? string.Empty,
				domicilio?.Departamento ?? string.Empty,
				domicilio?.Provincia ?? string.Empty,
				domicilio?.Distrito ?? string.Empty,
				domicilio?.AddressTypeCode ?? string.Empty,
				cliente.TipoCliente?.Codigo ?? string.Empty,
				cliente.RolCliente?.Codigo ?? string.Empty,
				foto?.NombreArchivo ?? string.Empty,
				foto?.UrlPublica ?? string.Empty
			};
		}
	}

	public sealed class ExportarClientesCompletoOutputDto
	{
		public ExportarClientesCompletoOutputDto(IReadOnlyList<string> cabeceras, IReadOnlyList<string[]> filas)
		{
			Cabeceras = cabeceras ?? Array.Empty<string>();
			Filas = filas ?? Array.Empty<string[]>();
		}

		public IReadOnlyList<string> Cabeceras { get; }
		public IReadOnlyList<string[]> Filas { get; }
	}
}
