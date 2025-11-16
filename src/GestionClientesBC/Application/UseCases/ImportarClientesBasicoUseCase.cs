using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Helpers;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Importar.Basico
{
	public interface IImportarClientesBasicoUseCase
	{
		Task<ImportarClientesBasicoOutputDto> Handle(ImportarClientesBasicoInputDto input, CancellationToken ct = default);
	}

	/// <summary>
	/// Importación básica (plantilla reducida) de clientes por Empresa.
	/// </summary>
	public sealed class ImportarClientesBasicoUseCase : IImportarClientesBasicoUseCase
	{
		private readonly IClienteRepository _repo;
		private readonly IUnitOfWork _uow;
		private readonly ITenantContext _tenant;

		public ImportarClientesBasicoUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<ImportarClientesBasicoOutputDto> Handle(ImportarClientesBasicoInputDto input, CancellationToken ct = default)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			if (input.Filas is null || input.Filas.Count == 0)
				throw new BusinessRuleException("No se recibieron filas para importar.");

			var empresaId = _tenant.EmpresaId;
			if (empresaId is null || empresaId.IsEmpty)
				throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

			var cachePorDocumento = new Dictionary<string, Cliente>(StringComparer.OrdinalIgnoreCase);
			int nuevos = 0;
			int actualizados = 0;

			foreach (var fila in input.Filas)
			{
				ct.ThrowIfCancellationRequested();

				var documento = CrearDocumento(fila.TipoDocumento, fila.NumeroDocumento);
				var key = Key(documento);

				if (!cachePorDocumento.TryGetValue(key, out var cliente))
				{
					var encontrados = await _repo.SearchAsync(empresaId, documento.Numero, null, null);
					cliente = encontrados.FirstOrDefault(c =>
						c.EmpresaId.EsMismaEmpresaQue(empresaId) &&
						c.Documento.Tipo == documento.Tipo &&
						string.Equals(c.Documento.Numero, documento.Numero, StringComparison.Ordinal));

					if (cliente is not null)
						cachePorDocumento[key] = cliente;
				}

				if (cliente is null)
				{
					var nuevo = CrearClienteNuevo(empresaId, documento, fila);
					nuevo.RegistrarImportacion("Basico", DateTime.UtcNow);
					await _repo.AddAsync(nuevo);
					cachePorDocumento[key] = nuevo;
					nuevos++;
					continue;
				}

				var expectedVersion = cliente.Version;
				var camposActualizados = AplicarCambiosBasicos(cliente, fila);

				if (camposActualizados.Count == 0)
					continue;

				cliente.RegistrarActualizacionMasiva(camposActualizados, DateTime.UtcNow);
				await _repo.UpdateAsync(cliente, expectedVersion);
				actualizados++;
			}

			await _uow.CommitAsync(ct);

			return new ImportarClientesBasicoOutputDto
			{
				TotalFilas = input.Filas.Count,
				Nuevos = nuevos,
				Actualizados = actualizados
			};
		}

		private static Cliente CrearClienteNuevo(EmpresaId empresaId, DocumentoIdentidad documento, ImportarClientesBasicoFilaDto fila)
		{
			RazonSocial? razonSocial = null;
			NombrePersona? nombres = null;

			if (documento.EsRuc)
			{
				if (string.IsNullOrWhiteSpace(fila.RazonSocial))
					throw new BusinessRuleException("Razón social obligatoria para RUC en importación básica.");
				razonSocial = RazonSocial.Crear(fila.RazonSocial!);
			}
			else
			{
				nombres = NombrePersonaInputMapper.CrearDesdeInput(
					fila.Nombres,
					fila.Apellidos,
					fila.NombresCompletos,
					"Los nombres son obligatorios para documento no RUC.");
			}

			Email? correo = string.IsNullOrWhiteSpace(fila.Correo) ? null : Email.Create(fila.Correo!);
			Telefono? telefono = string.IsNullOrWhiteSpace(fila.Telefonos) ? null : Telefono.FromTexto(fila.Telefonos!);

			var cliente = new Cliente(
				Guid.NewGuid(),
				empresaId,
				documento,
				razonSocial,
				nombres,
				correo,
				telefono,
				domicilioFiscal: null,
				tipoCliente: TipoCliente.Cliente,
				rolCliente: null,
				estado: EstadoCliente.Habilitado);

			return cliente;
		}

		private static List<string> AplicarCambiosBasicos(Cliente cliente, ImportarClientesBasicoFilaDto fila)
		{
			var cambios = new List<string>();

			if (cliente.Documento.EsRuc)
			{
				if (!string.IsNullOrWhiteSpace(fila.RazonSocial))
				{
					var nuevaRazon = RazonSocial.Crear(fila.RazonSocial!);
					if (!string.Equals(cliente.RazonSocial?.Valor, nuevaRazon.Valor, StringComparison.Ordinal))
					{
						cliente.ActualizarNombre(nuevaRazon);
						cambios.Add("RazonSocial");
					}
				}
			}
			else if (!string.IsNullOrWhiteSpace(fila.Nombres) ||
					 !string.IsNullOrWhiteSpace(fila.Apellidos) ||
					 !string.IsNullOrWhiteSpace(fila.NombresCompletos))
			{
				var nuevoNombre = NombrePersonaInputMapper.CrearDesdeInput(
					fila.Nombres,
					fila.Apellidos,
					fila.NombresCompletos,
					"Los nombres son obligatorios para documento no RUC.");

				if (!string.Equals(cliente.Nombres?.Completo, nuevoNombre.Completo, StringComparison.Ordinal))
				{
					cliente.ActualizarNombre(nuevoNombre);
					cambios.Add("Nombres");
				}
			}

			if (!string.IsNullOrWhiteSpace(fila.Correo) && !string.IsNullOrWhiteSpace(fila.Telefonos))
			{
				var nuevoCorreo = Email.Create(fila.Correo!);
				var nuevoTelefono = Telefono.FromTexto(fila.Telefonos!);
				var telefonoTexto = nuevoTelefono.UnirParaMostrar();

				bool requiereCambio = !string.Equals(cliente.Correo?.Value, nuevoCorreo.Value, StringComparison.OrdinalIgnoreCase)
					|| !string.Equals(cliente.Telefono?.UnirParaMostrar(), telefonoTexto, StringComparison.Ordinal);

				if (requiereCambio)
				{
					cliente.ActualizarDatosContacto(nuevoCorreo, telefonoTexto);
					cambios.Add("DatosContacto");
				}
			}

			return cambios;
		}

		private static DocumentoIdentidad CrearDocumento(string tipoDocumento, string numero)
		{
			if (string.IsNullOrWhiteSpace(tipoDocumento))
				throw new BusinessRuleException("TipoDocumento es obligatorio.");
			if (string.IsNullOrWhiteSpace(numero))
				throw new BusinessRuleException("NumeroDocumento es obligatorio.");

			if (!Enum.TryParse<TipoDocumento>(tipoDocumento, ignoreCase: true, out var tipo))
				throw new BusinessRuleException($"Tipo de documento no soportado: {tipoDocumento}");

			return DocumentoIdentidad.Crear(tipo, numero.Trim());
		}

		private static string Key(DocumentoIdentidad doc) => $"{doc.Tipo}:{doc.Numero}";
	}

	#region DTOs
	public sealed class ImportarClientesBasicoInputDto
	{
		public IReadOnlyCollection<ImportarClientesBasicoFilaDto> Filas { get; init; } = Array.Empty<ImportarClientesBasicoFilaDto>();
	}

	public class ImportarClientesBasicoFilaDto
	{
		public string TipoDocumento { get; init; } = null!;
		public string NumeroDocumento { get; init; } = null!;
		public string? RazonSocial { get; init; }
		public string? Nombres { get; init; }
		public string? Apellidos { get; init; }
		public string? NombresCompletos { get; init; }
		public string? Correo { get; init; }
		public string? Telefonos { get; init; }
	}

	public sealed class ImportarClientesBasicoOutputDto
	{
		public int TotalFilas { get; init; }
		public int Nuevos { get; init; }
		public int Actualizados { get; init; }
	}
	#endregion
}
