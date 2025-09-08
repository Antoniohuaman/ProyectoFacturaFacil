using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.Events;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Contactos.Agregar
{
    public interface IAgregarContactoClienteUseCase
    {
        Task<AgregarContactoClienteOutputDto> Handle(AgregarContactoClienteInputDto input, CancellationToken ct = default);
    }

    /// <summary>
    /// Agrega un <see cref="ContactoCliente"/> a la ficha de un cliente existente del tenant/empresa actual.
    /// </summary>
    public sealed class AgregarContactoClienteUseCase : IAgregarContactoClienteUseCase
    {
        private readonly IClienteRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public AgregarContactoClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
        {
            _repo   = repo   ?? throw new ArgumentNullException(nameof(repo));
            _uow    = uow    ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<AgregarContactoClienteOutputDto> Handle(AgregarContactoClienteInputDto input, CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ClienteId == Guid.Empty)
                throw new BusinessRuleException("ClienteId no puede ser vacío.");

            var empresaId = _tenant.EmpresaId;
            if (empresaId is null || empresaId.IsEmpty)
                throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

            // 1) Cargar agregado
            var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
            if (cliente is null)
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 2) Validar pertenencia a empresa
            if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
                throw NotFoundException.For<Cliente>(input.ClienteId);

            // 3) Construir Value Objects del contacto
            if (string.IsNullOrWhiteSpace(input.NombreContacto))
                throw new BusinessRuleException("El nombre del contacto no puede estar vacío.");

            var partesNombre = input.NombreContacto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partesNombre.Length < 2)
                throw new BusinessRuleException("El nombre del contacto debe incluir al menos nombre y apellidos.");

            var nombrePersona = partesNombre[0];
            var apellidosPersona = string.Join(" ", partesNombre.Skip(1));
            var nombre = SharedKernel.ValueObjects.NombrePersona.Crear(nombrePersona, apellidosPersona);

            DocumentoIdentidad? doc = null;
            bool algunDoc = input.TipoDocumentoContacto.HasValue || !string.IsNullOrWhiteSpace(input.NumeroDocumentoContacto);
            if (algunDoc)
            {
                if (!input.TipoDocumentoContacto.HasValue || string.IsNullOrWhiteSpace(input.NumeroDocumentoContacto))
                    throw new BusinessRuleException("Si se informa documento del contacto, se requiere TipoDocumentoContacto y NumeroDocumentoContacto.");

                if (input.TipoDocumentoContacto.Value != TipoDocumento.Dni)
                    throw new BusinessRuleException("El contacto secundario solo admite DNI como tipo de documento.");

                doc = DocumentoIdentidad.Crear(TipoDocumento.Dni, input.NumeroDocumentoContacto);
            }

            var emails = (input.Emails ?? new List<string>())
                .Select(SharedKernel.ValueObjects.Email.Create)
                .Distinct()
                .ToList();

            var telefonos = (input.Telefonos ?? new List<string>())
                .Select(SharedKernel.ValueObjects.Telefono.FromTexto)
                .ToList();

            // 4) Construir entidad contacto (validación adicional: el ctor impide doc != DNI)
            var contacto = new ContactoCliente(
                contactoId: Guid.NewGuid(),
                nombreContacto: nombre,
                documentoIdentidad: doc,
                emails: emails,
                telefonos: telefonos,
                direccion: input.Direccion
            );

            // 5) Agregar al agregado (verifica duplicados y registra ContactoAgregado)
            cliente.AgregarContacto(contacto);

            // 6) Persistir
            await _repo.UpdateAsync(cliente);
            await _uow.SaveChangesAsync(ct);

            // 7) Obtener el evento para timestamp
            var evt = cliente.DomainEvents.OfType<ContactoAgregado>().LastOrDefault();

            // 8) Salida
            return new AgregarContactoClienteOutputDto
            {
                ClienteId = cliente.ClienteId,
                EmpresaId = empresaId.Value,

                ContactoId = contacto.ContactoId,
                NombreContacto = contacto.NombreContacto.Completo,
                DocumentoIdentidad = contacto.DocumentoIdentidad?.ToString(),
                Emails = contacto.Emails.Select(e => e.Value).ToArray(),
                Telefonos = contacto.Telefonos.Select(t => t.UnirParaMostrar()).ToArray(),
                Direccion = contacto.Direccion,

                FechaCreacionUtc = contacto.FechaCreacion,
                FechaEventoUtc = evt?.OccurredOn ?? contacto.FechaCreacion
            };
        }
    }
}
