using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Clientes.Importar.Basico;
using GestionClientesBC.Application.Helpers;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Importar.Completo
{
	public interface IImportarClientesCompletoUseCase
	{
		Task<ImportarClientesCompletoOutputDto> Handle(ImportarClientesCompletoInputDto input, CancellationToken ct = default);
	}

	/// <summary>
	/// Importación completa: incluye direccionamiento, metadatos y foto.
	/// </summary>
	public sealed class ImportarClientesCompletoUseCase : IImportarClientesCompletoUseCase
	{
		private readonly IClienteRepository _repo;
		private readonly IUnitOfWork _uow;
		private readonly ITenantContext _tenant;

		public ImportarClientesCompletoUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<ImportarClientesCompletoOutputDto> Handle(ImportarClientesCompletoInputDto input, CancellationToken ct = default)
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));
			if (input.Filas is null || input.Filas.Count == 0)
				throw new BusinessRuleException("No se recibieron filas para importar.");

			var empresaId = _tenant.EmpresaId;
			if (empresaId is null || empresaId.IsEmpty)
				throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

			var cache = new Dictionary<string, Cliente>(StringComparer.OrdinalIgnoreCase);
			int nuevos = 0;
			int actualizados = 0;

			foreach (var fila in input.Filas)
			{
				ct.ThrowIfCancellationRequested();

				var documento = CrearDocumento(fila.TipoDocumento, fila.NumeroDocumento);
				var key = Key(documento);

				if (!cache.TryGetValue(key, out var cliente))
				{
					var encontrados = await _repo.SearchAsync(empresaId, documento.Numero, null, null);
					cliente = encontrados.FirstOrDefault(c =>
						c.EmpresaId.EsMismaEmpresaQue(empresaId) &&
						c.Documento.Tipo == documento.Tipo &&
						string.Equals(c.Documento.Numero, documento.Numero, StringComparison.Ordinal));

					if (cliente is not null)
						cache[key] = cliente;
				}

				if (cliente is null)
				{
					var nuevo = CrearClienteNuevo(empresaId, documento, fila);
					nuevo.RegistrarImportacion("Completo", DateTime.UtcNow);
					await _repo.AddAsync(nuevo);
					cache[key] = nuevo;
					nuevos++;
					continue;
				}

				var expectedVersion = cliente.Version;
				var campos = AplicarCambiosCompletos(cliente, fila);

				if (campos.Count == 0)
					continue;

				cliente.RegistrarActualizacionMasiva(campos, DateTime.UtcNow);
				await _repo.UpdateAsync(cliente, expectedVersion);
				actualizados++;
			}

			await _uow.CommitAsync(ct);

			return new ImportarClientesCompletoOutputDto
			{
				TotalFilas = input.Filas.Count,
				Nuevos = nuevos,
				Actualizados = actualizados
			};
		}

		private static Cliente CrearClienteNuevo(EmpresaId empresaId, DocumentoIdentidad documento, ImportarClientesCompletoFilaDto fila)
		{
			RazonSocial? razonSocial = null;
			NombrePersona? nombres = null;

			if (documento.EsRuc)
			{
				if (string.IsNullOrWhiteSpace(fila.RazonSocial))
					throw new BusinessRuleException("Razón social obligatoria para RUC en importación completa.");
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
			var domicilio = ConstruirDomicilio(fila);

			var tipoCliente = string.IsNullOrWhiteSpace(fila.TipoClienteCodigo)
				? TipoCliente.Cliente
				: TipoCliente.DesdeCodigo(fila.TipoClienteCodigo);
			RolCliente? rolCliente = string.IsNullOrWhiteSpace(fila.RolClienteCodigo)
				? null
				: RolCliente.DesdeCodigo(fila.RolClienteCodigo);

			var nombreComercial = string.IsNullOrWhiteSpace(fila.NombreComercial)
				? null
				: NombreCliente.Crear(fila.NombreComercial!);
			var paginaWeb = PaginaWebCliente.Create(fila.PaginaWeb);
			var observaciones = ObservacionesCliente.Create(fila.Observaciones);
			var fotoPerfil = FotoPerfilCliente.Create(fila.FotoPerfilNombreArchivo, fila.FotoPerfilUrl);

			return new Cliente(
				Guid.NewGuid(),
				empresaId,
				documento,
				razonSocial,
				nombres,
				correo,
				telefono,
				domicilio,
				tipoCliente,
				rolCliente,
				estado: EstadoCliente.Habilitado,
				nombreComercial: nombreComercial,
				paginaWeb: paginaWeb,
				observaciones: observaciones,
				fotoPerfil: fotoPerfil,
				datosSunat: null);
		}

		private static List<string> AplicarCambiosCompletos(Cliente cliente, ImportarClientesCompletoFilaDto fila)
		{
			var cambios = AplicarCambiosBasicos(cliente, fila);

			if (!string.IsNullOrWhiteSpace(fila.NombreComercial))
			{
				var nuevoNombre = NombreCliente.Crear(fila.NombreComercial!);
				if (!Equals(cliente.NombreComercial, nuevoNombre))
				{
					cliente.ActualizarNombreComercial(nuevoNombre);
					cambios.Add("NombreComercial");
				}
			}

			if (!string.IsNullOrWhiteSpace(fila.PaginaWeb))
			{
				var nuevaPagina = PaginaWebCliente.Create(fila.PaginaWeb);
				if (!Equals(cliente.PaginaWeb, nuevaPagina))
				{
					cliente.ActualizarPaginaWeb(nuevaPagina);
					cambios.Add("PaginaWeb");
				}
			}

			if (!string.IsNullOrWhiteSpace(fila.Observaciones))
			{
				var nuevasObs = ObservacionesCliente.Create(fila.Observaciones);
				if (!Equals(cliente.Observaciones, nuevasObs))
				{
					cliente.ActualizarObservaciones(nuevasObs);
					cambios.Add("Observaciones");
				}
			}

			var direccion = ConstruirDomicilio(fila);
			if (direccion is not null && !Equals(cliente.DomicilioFiscal, direccion))
			{
				cliente.ActualizarDireccion(direccion);
				cambios.Add("Direccion");
			}

			if (!string.IsNullOrWhiteSpace(fila.TipoClienteCodigo))
			{
				var nuevoTipo = TipoCliente.DesdeCodigo(fila.TipoClienteCodigo);
				if (!Equals(cliente.TipoCliente, nuevoTipo))
				{
					cliente.ActualizarTipoCliente(nuevoTipo);
					cambios.Add("TipoCliente");
				}
			}

			if (!string.IsNullOrWhiteSpace(fila.RolClienteCodigo))
			{
				var nuevoRol = RolCliente.DesdeCodigo(fila.RolClienteCodigo);
				if (!Equals(cliente.RolCliente, nuevoRol))
				{
					cliente.ActualizarRolCliente(nuevoRol);
					cambios.Add("RolCliente");
				}
			}

			if (!string.IsNullOrWhiteSpace(fila.FotoPerfilNombreArchivo) || !string.IsNullOrWhiteSpace(fila.FotoPerfilUrl))
			{
				var nuevaFoto = FotoPerfilCliente.Create(fila.FotoPerfilNombreArchivo, fila.FotoPerfilUrl);
				if (!Equals(cliente.FotoPerfil, nuevaFoto))
				{
					cliente.ActualizarFotoPerfil(nuevaFoto);
					cambios.Add("FotoPerfil");
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

		private static DomicilioFiscal? ConstruirDomicilio(ImportarClientesCompletoFilaDto fila)
		{
			bool hayDireccion =
				!string.IsNullOrWhiteSpace(fila.DireccionLinea) ||
				!string.IsNullOrWhiteSpace(fila.Ubigeo) ||
				!string.IsNullOrWhiteSpace(fila.Departamento) ||
				!string.IsNullOrWhiteSpace(fila.Provincia) ||
				!string.IsNullOrWhiteSpace(fila.Distrito) ||
				!string.IsNullOrWhiteSpace(fila.AddressTypeCode);

			var pais = string.IsNullOrWhiteSpace(fila.PaisCodigoIso) ? "PE" : fila.PaisCodigoIso!.Trim().ToUpperInvariant();

			if (!hayDireccion && string.Equals(pais, "PE", StringComparison.OrdinalIgnoreCase))
				return null;

			return string.Equals(pais, "PE", StringComparison.OrdinalIgnoreCase)
				? DomicilioFiscal.FromPeru(
					linea: fila.DireccionLinea,
					ubigeo: fila.Ubigeo,
					departamento: fila.Departamento,
					provincia: fila.Provincia,
					distrito: fila.Distrito,
					addressTypeCode: fila.AddressTypeCode)
				: DomicilioFiscal.From(
					paisCodigoIso: pais,
					linea: fila.DireccionLinea,
					ubigeo: fila.Ubigeo,
					departamento: fila.Departamento,
					provincia: fila.Provincia,
					distrito: fila.Distrito,
					addressTypeCode: fila.AddressTypeCode);
		}

		private static List<string> AplicarCambiosBasicos(Cliente cliente, ImportarClientesCompletoFilaDto fila)
		{
			var cambios = new List<string>();

			if (cliente.Documento.EsRuc)
			{
				if (!string.IsNullOrWhiteSpace(fila.RazonSocial))
				{
					var nueva = RazonSocial.Crear(fila.RazonSocial!);
					if (!string.Equals(cliente.RazonSocial?.Valor, nueva.Valor, StringComparison.Ordinal))
					{
						cliente.ActualizarNombre(nueva);
						cambios.Add("RazonSocial");
					}
				}
			}
			else if (!string.IsNullOrWhiteSpace(fila.Nombres) ||
					 !string.IsNullOrWhiteSpace(fila.Apellidos) ||
					 !string.IsNullOrWhiteSpace(fila.NombresCompletos))
			{
				var nuevo = NombrePersonaInputMapper.CrearDesdeInput(
					fila.Nombres,
					fila.Apellidos,
					fila.NombresCompletos,
					"Los nombres son obligatorios para documento no RUC.");

				if (!string.Equals(cliente.Nombres?.Completo, nuevo.Completo, StringComparison.Ordinal))
				{
					cliente.ActualizarNombre(nuevo);
					cambios.Add("Nombres");
				}
			}

			var correoNuevo = string.IsNullOrWhiteSpace(fila.Correo) ? null : Email.Create(fila.Correo!);
			var telefonosNuevos = string.IsNullOrWhiteSpace(fila.Telefonos) ? null : fila.Telefonos;
			if (cliente.ActualizarDatosContacto(correoNuevo, telefonosNuevos))
			{
				cambios.Add("DatosContacto");
			}

			return cambios;
		}
	}

	#region DTOs
	public sealed class ImportarClientesCompletoInputDto
	{
		public IReadOnlyCollection<ImportarClientesCompletoFilaDto> Filas { get; init; } = Array.Empty<ImportarClientesCompletoFilaDto>();
	}

	public sealed class ImportarClientesCompletoFilaDto : ImportarClientesBasicoFilaDto
	{
		public string? NombreComercial { get; init; }
		public string? PaginaWeb { get; init; }
		public string? Observaciones { get; init; }
		public string? PaisCodigoIso { get; init; }
		public string? DireccionLinea { get; init; }
		public string? Ubigeo { get; init; }
		public string? Departamento { get; init; }
		public string? Provincia { get; init; }
		public string? Distrito { get; init; }
		public string? AddressTypeCode { get; init; }
		public string? TipoClienteCodigo { get; init; }
		public string? RolClienteCodigo { get; init; }
		public string? FotoPerfilNombreArchivo { get; init; }
		public string? FotoPerfilUrl { get; init; }
	}

	public sealed class ImportarClientesCompletoOutputDto
	{
		public int TotalFilas { get; init; }
		public int Nuevos { get; init; }
		public int Actualizados { get; init; }
	}
	#endregion
}
