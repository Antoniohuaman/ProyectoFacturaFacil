using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
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
                        var nombresTxt = input.NombresCompletos ?? cliente.Nombres?.Completo;
                        if (string.IsNullOrWhiteSpace(nombresTxt))
                            throw new BusinessRuleException("Para documentos distintos de RUC se requieren nombres válidos.");
                    }

                    cambios["Documento"] = (cliente.Documento.ToString(), nuevoDoc.ToString());
                    cliente.ActualizarDocumentoIdentidad(nuevoDoc);
                }
            }

            // 4) Nombre / Razón social (opcional)
            if (!string.IsNullOrWhiteSpace(input.RazonSocial) || !string.IsNullOrWhiteSpace(input.NombresCompletos))
            {
                if (cliente.Documento.EsRuc)
                {
                    if (string.IsNullOrWhiteSpace(input.RazonSocial))
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
                    if (string.IsNullOrWhiteSpace(input.NombresCompletos))
                        throw new BusinessRuleException("Para documento no RUC debe proporcionar nombres.");
                    // Se asume que input.NombresCompletos contiene ambos nombres y apellidos separados por espacio
                    var nombresSplit = input.NombresCompletos!.Trim().Split(' ', 2);
                    var nombre = nombresSplit.Length > 0 ? nombresSplit[0] : string.Empty;
                    var apellidos = nombresSplit.Length > 1 ? nombresSplit[1] : string.Empty;
                    var nuevo = SharedKernel.ValueObjects.NombrePersona.Crear(nombre, apellidos);
                    if (cliente.Nombres is null || !string.Equals(cliente.Nombres.Completo, nuevo.Completo, StringComparison.Ordinal))
                    {
                        cambios["Nombres"] = (cliente.Nombres?.Completo, nuevo.Completo);
                        cliente.ActualizarNombre(nuevo);
                    }
                }
            }

            // 5) Contacto (correo/teléfonos) - requiere ambos valores para invocar el método del agregado
            bool quiereCorreo = !string.IsNullOrWhiteSpace(input.Correo);
            bool quiereTel    = !string.IsNullOrWhiteSpace(input.Telefonos);

            if (quiereCorreo || quiereTel)
            {
                var correoNuevo = quiereCorreo ? Email.Create(input.Correo!) : cliente.Correo
                    ?? throw new BusinessRuleException("Para actualizar solo teléfono, el cliente debe tener un correo actual.");
                var telNuevoStr = quiereTel ? input.Telefonos! : (cliente.Telefono?.UnirParaMostrar()
                    ?? throw new BusinessRuleException("Para actualizar solo correo, el cliente debe tener un teléfono actual."));

                var correoAnterior = cliente.Correo?.Value;
                var telAnterior    = cliente.Telefono?.UnirParaMostrar();

                cliente.ActualizarDatosContacto(correoNuevo, telNuevoStr);

                if (!string.Equals(correoAnterior, cliente.Correo?.Value, StringComparison.Ordinal))
                    cambios["Correo"] = (correoAnterior, cliente.Correo?.Value);
                if (!string.Equals(telAnterior, cliente.Telefono?.UnirParaMostrar(), StringComparison.Ordinal))
                    cambios["Telefono"] = (telAnterior, cliente.Telefono?.UnirParaMostrar());
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

            // 8) Estado (opcional)
            if (input.Habilitado.HasValue)
            {
                var estabaHabilitado = cliente.Estado?.EsHabilitado ?? false;
                if (input.Habilitado.Value && !estabaHabilitado)
                {
                    cliente.Habilitar();
                    cambios["Estado"] = ("INH", "HAB");
                }
                else if (!input.Habilitado.Value && estabaHabilitado)
                {
                    var motivo = input.MotivoDeshabilitacion; // puede ser null
                    cliente.Deshabilitar(motivo, DateTime.UtcNow);
                    cambios["Estado"] = ("HAB", "INH");
                    if (!string.IsNullOrWhiteSpace(motivo))
                        cambios["MotivoDeshabilitacion"] = (null, motivo);
                }
            }

            // 9) Trazabilidad (si hubo cambios)
            if (cambios.Count > 0)
            {
                cliente.RegistrarModificacion(cambios);
                await _repo.UpdateAsync(cliente);
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
