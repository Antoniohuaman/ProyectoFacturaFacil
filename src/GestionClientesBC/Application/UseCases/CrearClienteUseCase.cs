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
            var docId = new DocumentoIdentidad(
                Enum.Parse<TipoDocumento>(dto.TipoDocumento),
                dto.NumeroDocumento);

            var existente = await _repo.GetByDocumentoIdentidadAsync(docId);
            if (existente != null)
                throw new InvalidOperationException("Cliente ya existe.");

            var cliente = new Cliente(
                Guid.NewGuid(),
                docId,
                dto.RazonSocialONombres,
                dto.Correo,
                dto.Celular,
                dto.DireccionPostal,
                Enum.Parse<TipoCliente>(dto.TipoCliente),
                EstadoCliente.Activo
                // Ya no se pasa FechaRegistro
            );

            await _repo.AddAsync(cliente);
            await _uow.CommitAsync();

            return cliente.ClienteId;
        }
    }
}