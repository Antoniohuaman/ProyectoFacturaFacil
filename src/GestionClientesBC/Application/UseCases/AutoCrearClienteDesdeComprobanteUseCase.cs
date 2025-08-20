using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Application.UseCases
{
    public class AutoCrearClienteDesdeComprobanteUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public AutoCrearClienteDesdeComprobanteUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<Guid?> HandleAsync(AutoCrearClienteDesdeComprobanteDto dto)
        {
            // Validar documento mínimo
            if (string.IsNullOrWhiteSpace(dto.TipoDocumento) || string.IsNullOrWhiteSpace(dto.NumeroDocumento))
                return null;

            // Validar formato de documento (VO lo valida, si falla, descarta)
            DocumentoIdentidad docId;
            try
            {
                docId = new DocumentoIdentidad(
                    Enum.Parse<TipoDocumento>(dto.TipoDocumento),
                    dto.NumeroDocumento);
            }
            catch
            {
                // Documento inválido, no crear cliente
                return null;
            }

            // Verificar si ya existe cliente con ese documento
            var existente = await _repo.GetByDocumentoIdentidadAsync(docId);
            if (existente != null)
                return existente.ClienteId;

            // Crear cliente mínimo
            var direccionPostal = DireccionPostal.From(
                paisCodigoIso: "PE",
                linea: dto.DireccionPostal ?? string.Empty,
                ubigeo: string.Empty,
                departamento: string.Empty,
                provincia: string.Empty,
                distrito: string.Empty,
                addressTypeCode: "0000"
            );

            var cliente = new Cliente(
                Guid.NewGuid(),
                docId,
                string.IsNullOrWhiteSpace(dto.RazonSocialONombres) ? "Cliente sin nombre" : dto.RazonSocialONombres,
                null, // correo
                null, // celular
                direccionPostal,
                TipoCliente.SinDefinir,
                EstadoCliente.Activo
            );

            await _repo.AddAsync(cliente);
            await _uow.CommitAsync();

            // Aquí se publicaría el evento ClienteCreado (si tienes un bus de eventos, publícalo aquí)

            return cliente.ClienteId;
        }
    }
}