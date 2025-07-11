using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Application.UseCases
{
    public class CrearClienteUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public CrearClienteUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<Guid> HandleAsync(ClienteDto dto)
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(dto.TipoDocumento))
                throw new ArgumentException("Tipo de documento es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.NumeroDocumento))
                throw new ArgumentException("Número de documento es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.RazonSocialONombres))
                throw new ArgumentException("Nombre o razón social es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.TipoCliente))
                throw new ArgumentException("Tipo de cliente es obligatorio.");

            // Validar formato de correo si viene
            if (!string.IsNullOrWhiteSpace(dto.Correo) && !CorreoValido(dto.Correo))
                throw new ArgumentException("El correo electrónico no tiene formato válido.");

            // Validar formato de documento (VO lo valida)
            var docId = new DocumentoIdentidad(
                Enum.Parse<TipoDocumento>(dto.TipoDocumento),
                dto.NumeroDocumento);

            // Validar unicidad del documento
            var existente = await _repo.GetByDocumentoIdentidadAsync(docId);
            if (existente != null)
                throw new InvalidOperationException("Cliente ya existe.");

            var cliente = new Cliente(
                Guid.NewGuid(),
                docId,
                dto.RazonSocialONombres!,
                dto.Correo,
                dto.Celular,
                dto.DireccionPostal,
                Enum.Parse<TipoCliente>(dto.TipoCliente!), // El ! es seguro porque ya validaste arriba
                EstadoCliente.Activo
            );

            await _repo.AddAsync(cliente);
            await _uow.CommitAsync();

            return cliente.ClienteId;
        }

        private bool CorreoValido(string correo)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(correo);
                return addr.Address == correo;
            }
            catch
            {
                return false;
            }
        }
    }
}