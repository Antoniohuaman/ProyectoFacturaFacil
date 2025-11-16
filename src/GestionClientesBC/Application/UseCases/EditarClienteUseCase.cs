using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Application.Helpers;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Editar
{
    public interface IEditarClienteUseCase
    {
        Task<EditarClienteOutputDto> Handle(EditarClienteInputDto input, CancellationToken ct = default);
    }

    public sealed class EditarClienteUseCase : IEditarClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public EditarClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow  = uow  ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<EditarClienteOutputDto> Handle(EditarClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar agregado
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw NotFoundException.For<Cliente>(input.ClienteId);
            var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
            if (cliente is null)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 2) Verificar tenant
            if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
                throw NotFoundException.For<Cliente>(input.ClienteId); // no exponer existencia en otro tenant

            // Snapshot para diffs
            var cambios = new Dictionary<string, (object? anterior, object? nuevo)>(StringComparer.Ordinal);

            // 3) Documento (opcional) + revalidaciones de nombre
            if (input.TipoDocumento.HasValue || !string.IsNullOrWhiteSpace(input.NumeroDocumento))
            {
                var nuevoTipo   = input.TipoDocumento ?? cliente.Documento.Tipo;
                var nuevoNumero = string.IsNullOrWhiteSpace(input.NumeroDocumento)
                    ? cliente.Documento.Numero
                    : input.NumeroDocumento!;

                var nuevoDoc = DocumentoIdentidad.Crear(nuevoTipo, nuevoNumero);

                // Evitar trabajo si no cambia
                if (!nuevoDoc.Equals(cliente.Documento))
                {
                    // Unicidad por Empresa + Documento
                    var posibles = await _repo.SearchAsync(_tenant.EmpresaId, nuevoDoc.Numero, null, null);
                    bool duplicado = posibles.Any(c =>
                        c.ClienteId != cliente.ClienteId &&
                        c.EmpresaId.EsMismaEmpresaQue(empresaId) &&
                        c.Documento.Tipo == nuevoDoc.Tipo &&
                        string.Equals(c.Documento.Numero, nuevoDoc.Numero, StringComparison.Ordinal));

                    if (duplicado)
                        throw new BusinessRuleException("Ya existe un cliente con el mismo documento en esta empresa.");

                    // Reglas de nombre según tipo
                    if (nuevoDoc.EsRuc)
                    {
                        // usar input si vino, sino el actual
                        var rs = input.RazonSocial ?? cliente.RazonSocial?.Valor;
                        if (string.IsNullOrWhiteSpace(rs))
                            throw new BusinessRuleException("Para RUC se requiere una razón social válida.");
                        // No asignamos aquí nombre; se actualiza más abajo si pidió cambiarlo.
                    }
                    else
                    {
                        var tieneNombresNuevos =
                            (!string.IsNullOrWhiteSpace(input.Nombres) && !string.IsNullOrWhiteSpace(input.Apellidos)) ||
                            !string.IsNullOrWhiteSpace(input.NombresCompletos);

                        if (!tieneNombresNuevos && (cliente.Nombres is null || string.IsNullOrWhiteSpace(cliente.Nombres.Completo)))
                            throw new BusinessRuleException("Para documentos distintos de RUC se requieren nombres válidos.");
                    }

                    cambios["Documento"] = (cliente.Documento.ToString(), nuevoDoc.ToString());
                    cliente.ActualizarDocumentoIdentidad(nuevoDoc);
                }
            }

            // 4) Nombre / Razón social (opcional)
            bool quiereActualizarRazon = input.RazonSocial is not null;
            bool quiereActualizarNombrePersona =
                input.Nombres is not null ||
                input.Apellidos is not null ||
                input.NombresCompletos is not null;

            if (quiereActualizarRazon || quiereActualizarNombrePersona)
            {
                if (cliente.Documento.EsRuc)
                {
                    if (!quiereActualizarRazon || string.IsNullOrWhiteSpace(input.RazonSocial))
                        throw new BusinessRuleException("Para RUC debe proporcionar una razón social.");
                    var nuevo = SharedKernel.ValueObjects.RazonSocial.Crear(input.RazonSocial!);
                    if (cliente.RazonSocial is null || !string.Equals(cliente.RazonSocial.Valor, nuevo.Valor, StringComparison.Ordinal))
                    {
                        cambios["RazonSocial"] = (cliente.RazonSocial?.Valor, nuevo.Valor);
                        cliente.ActualizarNombre(nuevo);
                    }
                }
                else
                {
                    if (!quiereActualizarNombrePersona)
                        throw new BusinessRuleException("Para documento no RUC debe proporcionar nombres.");

                    var nuevo = NombrePersonaInputMapper.CrearDesdeInput(
                        input.Nombres,
                        input.Apellidos,
                        input.NombresCompletos,
                        "Para documento no RUC debe proporcionar nombres.");

                    if (cliente.Nombres is null || !string.Equals(cliente.Nombres.Completo, nuevo.Completo, StringComparison.Ordinal))
                    {
                        cambios["Nombres"] = (cliente.Nombres?.Completo, nuevo.Completo);
                        cliente.ActualizarNombre(nuevo);
                    }
                }
            }

            // 5) Contacto (correo/teléfonos)
            bool quiereCorreo = !string.IsNullOrWhiteSpace(input.Correo);
            bool quiereTel    = !string.IsNullOrWhiteSpace(input.Telefonos);

            if (quiereCorreo || quiereTel)
            {
                var correoNuevo = quiereCorreo ? Email.Create(input.Correo!) : null;
                var telNuevoStr = quiereTel ? input.Telefonos : null;

                var correoAnterior = cliente.Correo?.Value;
                var telAnterior    = cliente.Telefono?.UnirParaMostrar();

                if (cliente.ActualizarDatosContacto(correoNuevo, telNuevoStr))
                {
                    var correoActual = cliente.Correo?.Value;
                    var telActual    = cliente.Telefono?.UnirParaMostrar();

                    if (!string.Equals(correoAnterior, correoActual, StringComparison.OrdinalIgnoreCase))
                        cambios["Correo"] = (correoAnterior, correoActual);
                    if (!string.Equals(telAnterior, telActual, StringComparison.Ordinal))
                        cambios["Telefono"] = (telAnterior, telActual);
                }
            }

            // 6) Dirección (opcional)
            bool hayDireccion =
                !string.IsNullOrWhiteSpace(input.DireccionLinea) ||
                !string.IsNullOrWhiteSpace(input.Ubigeo) ||
                !string.IsNullOrWhiteSpace(input.Departamento) ||
                !string.IsNullOrWhiteSpace(input.Provincia) ||
                !string.IsNullOrWhiteSpace(input.Distrito) ||
                !string.IsNullOrWhiteSpace(input.AddressTypeCode);

            if (hayDireccion)
            {
                var pais = string.IsNullOrWhiteSpace(input.PaisCodigoIso) ? "PE" : input.PaisCodigoIso!.Trim().ToUpperInvariant();
                var nuevaDir = string.Equals(pais, "PE", StringComparison.OrdinalIgnoreCase)
                    ? DomicilioFiscal.FromPeru(
                        linea: input.DireccionLinea,
                        ubigeo: input.Ubigeo,
                        departamento: input.Departamento,
                        provincia: input.Provincia,
                        distrito: input.Distrito,
                        addressTypeCode: input.AddressTypeCode)
                    : DomicilioFiscal.From(
                        paisCodigoIso: pais,
                        linea: input.DireccionLinea,
                        ubigeo: input.Ubigeo,
                        departamento: input.Departamento,
                        provincia: input.Provincia,
                        distrito: input.Distrito,
                        addressTypeCode: input.AddressTypeCode);

                if (!Equals(cliente.DomicilioFiscal, nuevaDir))
                {
                    cambios["DomicilioFiscal"] = (cliente.DomicilioFiscal?.ToString(), nuevaDir.ToString());
                    cliente.ActualizarDireccion(nuevaDir);
                }
            }

            // 7) Segmentación (opcionales)
            if (!string.IsNullOrWhiteSpace(input.TipoClienteCodigo))
            {
                var nuevoTipo = TipoCliente.DesdeCodigo(input.TipoClienteCodigo);
                if (!Equals(cliente.TipoCliente, nuevoTipo))
                {
                    cambios["TipoCliente"] = (cliente.TipoCliente?.Codigo, nuevoTipo.Codigo);
                    cliente.ActualizarTipoCliente(nuevoTipo);
                }
            }

            if (input.RemoverRolCliente == true || !string.IsNullOrWhiteSpace(input.RolClienteCodigo))
            {
                RolCliente? nuevoRol = input.RemoverRolCliente == true
                    ? null
                    : RolCliente.DesdeCodigo(input.RolClienteCodigo);

                // Comparar por referencia (instancias conocidas) o por código si alguna es null
                var anteriorCod = cliente.RolCliente?.Codigo;
                var nuevoCod    = nuevoRol?.Codigo;

                if (!string.Equals(anteriorCod, nuevoCod, StringComparison.Ordinal))
                {
                    cambios["RolCliente"] = (anteriorCod, nuevoCod);
                    cliente.ActualizarRolCliente(nuevoRol);
                }
            }

            if (input.NombreComercial is not null)
            {
                var nuevoNombreComercial = string.IsNullOrWhiteSpace(input.NombreComercial)
                    ? null
                    : NombreCliente.Crear(input.NombreComercial);

                if (!Equals(cliente.NombreComercial, nuevoNombreComercial))
                {
                    cambios["NombreComercial"] = (cliente.NombreComercial?.ParaMostrar, nuevoNombreComercial?.ParaMostrar);
                    cliente.ActualizarNombreComercial(nuevoNombreComercial);
                }
            }

            if (input.PaginaWeb is not null)
            {
                var nuevaPaginaWeb = PaginaWebCliente.Create(input.PaginaWeb);
                if (!Equals(cliente.PaginaWeb, nuevaPaginaWeb))
                {
                    cambios["PaginaWeb"] = (cliente.PaginaWeb?.Valor, nuevaPaginaWeb?.Valor);
                    cliente.ActualizarPaginaWeb(nuevaPaginaWeb);
                }
            }

            if (input.Observaciones is not null)
            {
                var nuevasObservaciones = ObservacionesCliente.Create(input.Observaciones);
                if (!Equals(cliente.Observaciones, nuevasObservaciones))
                {
                    cambios["Observaciones"] = (cliente.Observaciones?.Valor, nuevasObservaciones?.Valor);
                    cliente.ActualizarObservaciones(nuevasObservaciones);
                }
            }

            if (input.FotoPerfilNombreArchivo is not null || input.FotoPerfilUrl is not null)
            {
                var nuevoFotoPerfil = string.IsNullOrWhiteSpace(input.FotoPerfilNombreArchivo) && string.IsNullOrWhiteSpace(input.FotoPerfilUrl)
                    ? null
                    : FotoPerfilCliente.Create(input.FotoPerfilNombreArchivo, input.FotoPerfilUrl);

                if (!Equals(cliente.FotoPerfil, nuevoFotoPerfil))
                {
                    cambios["FotoPerfil"] = (cliente.FotoPerfil?.ToString(), nuevoFotoPerfil?.ToString());
                    cliente.ActualizarFotoPerfil(nuevoFotoPerfil);
                }
            }

            // 8) Estado (opcional)
            if (input.Habilitado.HasValue)
            {
                var estabaHabilitado = cliente.Estado?.EsHabilitado ?? false;
                if (input.Habilitado.Value && !estabaHabilitado)
                {
                    cliente.Habilitar();
                    cambios["Estado"] = ("DES", "HAB");
                }
                else if (!input.Habilitado.Value && estabaHabilitado)
                {
                    var motivo = MotivoDeshabilitacionCliente.Create(input.MotivoDeshabilitacion);
                    cliente.Deshabilitar(motivo, DateTime.UtcNow);
                    cambios["Estado"] = ("HAB", "DES");
                    if (motivo is not null)
                        cambios["MotivoDeshabilitacion"] = (null, motivo.Valor);
                }
            }

            // 9) Trazabilidad (si hubo cambios)
            if (cambios.Count > 0)
            {
                cliente.RegistrarModificacion(cambios);
                var expectedVersion = input.ExpectedVersion ?? cliente.Version;
                await _repo.UpdateAsync(cliente, expectedVersion);
                await _uow.CommitAsync(ct);
            }

            // 10) Salida
            return new EditarClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,
                TipoDocumento = cliente.Documento.Tipo.ToString(),
                NumeroDocumento = cliente.Documento.Numero,
                RazonSocial = cliente.RazonSocial?.Valor,
                Nombres = cliente.Nombres?.Completo ?? string.Empty,
                NombreComercial = cliente.NombreComercial?.ParaMostrar,
                PaginaWeb = cliente.PaginaWeb?.Valor,
                Observaciones = cliente.Observaciones?.Valor,
                FotoPerfilNombreArchivo = cliente.FotoPerfil?.NombreArchivo,
                FotoPerfilUrl = cliente.FotoPerfil?.UrlPublica,
                Correo = cliente.Correo?.Value,
                Telefonos = cliente.Telefono?.UnirParaMostrar(),
                TipoCliente = cliente.TipoCliente?.Codigo,
                RolCliente = cliente.RolCliente?.Codigo,
                Estado = cliente.Estado?.Codigo,
                FechaRegistroUtc = cliente.FechaRegistro,
                FechaUltimaModificacionUtc = cliente.FechaUltimaModificacion
            };
        }
    }
}
