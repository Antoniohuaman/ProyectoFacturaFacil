using System;
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

namespace GestionClientesBC.Application.Clientes.Crear
{
    public interface ICrearClienteUseCase
    {
        Task<CrearClienteOutputDto> Handle(CrearClienteInputDto input, CancellationToken ct = default);
    }

    public sealed class CrearClienteUseCase : ICrearClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public CrearClienteUseCase(
            IClienteRepository repo,
            IUnitOfWork uow,
            ITenantContext tenant)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<CrearClienteOutputDto> Handle(CrearClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Empresa actual (multitenant)
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

            // 2) Documento VO
            var documento = DocumentoIdentidad.Crear(input.TipoDocumento, input.NumeroDocumento);

            // 3) Nombre / Razón social según tipo
            RazonSocial? razonSocial = null;
            NombrePersona? nombres = null;

            if (documento.EsRuc)
            {
                if (string.IsNullOrWhiteSpace(input.RazonSocial))
                    throw new BusinessRuleException("La razón social es obligatoria para RUC.");
                razonSocial = SharedKernel.ValueObjects.RazonSocial.Crear(input.RazonSocial!);
            }
            else
            {
                nombres = NombrePersonaInputMapper.CrearDesdeInput(
                    input.Nombres,
                    input.Apellidos,
                    input.NombresCompletos,
                    "Los nombres son obligatorios para documento no RUC.");
            }

            // 4) Detección de duplicado por EmpresaId + Documento (usando SearchAsync existente)
            var posibles = await _repo.SearchAsync(empresaId, documento.Numero, skip: null, take: null);
            bool existeDuplicado = posibles.Any(c =>
                c.EmpresaId.EsMismaEmpresaQue(empresaId) &&
                c.Documento.Tipo == documento.Tipo &&
                string.Equals(c.Documento.Numero, documento.Numero, StringComparison.Ordinal));

            if (existeDuplicado)
                throw new BusinessRuleException("Ya existe un cliente con el mismo documento en esta empresa.");

            // 5) Opcionales
            Email? correo = null;
            if (!string.IsNullOrWhiteSpace(input.Correo))
                correo = Email.Create(input.Correo!);

            Telefono? telefono = null;
            if (!string.IsNullOrWhiteSpace(input.Telefonos))
                telefono = Telefono.FromTexto(input.Telefonos);

            DomicilioFiscal? domicilio = null;
            bool hayDireccion =
                !string.IsNullOrWhiteSpace(input.DireccionLinea) ||
                !string.IsNullOrWhiteSpace(input.Ubigeo) ||
                !string.IsNullOrWhiteSpace(input.Departamento) ||
                !string.IsNullOrWhiteSpace(input.Provincia) ||
                !string.IsNullOrWhiteSpace(input.Distrito) ||
                !string.IsNullOrWhiteSpace(input.AddressTypeCode);

            var pais = string.IsNullOrWhiteSpace(input.PaisCodigoIso) ? "PE" : input.PaisCodigoIso!.Trim().ToUpperInvariant();

            if (hayDireccion || !string.Equals(pais, "PE", StringComparison.OrdinalIgnoreCase))
            {
                domicilio = string.Equals(pais, "PE", StringComparison.OrdinalIgnoreCase)
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
            }

            // 6) Segmentación
            var tipoCliente = string.IsNullOrWhiteSpace(input.TipoClienteCodigo)
                ? TipoCliente.Cliente
                : TipoCliente.DesdeCodigo(input.TipoClienteCodigo);

            RolCliente? rolCliente = null;
            if (!string.IsNullOrWhiteSpace(input.RolClienteCodigo))
                rolCliente = RolCliente.DesdeCodigo(input.RolClienteCodigo);

            // 7) Metadatos opcionales
            NombreCliente? nombreComercial = null;
            if (!string.IsNullOrWhiteSpace(input.NombreComercial))
                nombreComercial = NombreCliente.Crear(input.NombreComercial);

            var paginaWeb = PaginaWebCliente.Create(input.PaginaWeb);
            var observaciones = ObservacionesCliente.Create(input.Observaciones);
            var fotoPerfil = FotoPerfilCliente.Create(input.FotoPerfilNombreArchivo, input.FotoPerfilUrl);

            // 8) Crear agregado y persistir
            var cliente = new Cliente(
                clienteId: Guid.NewGuid(),
                empresaId: empresaId,
                documento: documento,
                razonSocial: razonSocial,
                nombres: nombres,
                correo: correo,
                telefono: telefono,
                domicilioFiscal: domicilio,
                tipoCliente: tipoCliente,
                rolCliente: rolCliente,
                estado: EstadoCliente.Habilitado,
                nombreComercial: nombreComercial,
                paginaWeb: paginaWeb,
                observaciones: observaciones,
                fotoPerfil: fotoPerfil,
                datosSunat: null
            );

            await _repo.AddAsync(cliente);
            await _uow.CommitAsync(ct);

            // 9) Salida
            return new CrearClienteOutputDto
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
                Estado = cliente.Estado!.Nombre,
                FechaRegistroUtc = cliente.FechaRegistro
            };
        }
    }
}
