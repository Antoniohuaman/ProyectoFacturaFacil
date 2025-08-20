using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
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

            // Construir value object DireccionPostal desde el DTO estructurado
            DireccionPostal direccionPostal;
            if (dto.DireccionPostal != null)
            {
                direccionPostal = DireccionPostal.From(
                    paisCodigoIso: dto.DireccionPostal.PaisCodigoIso ?? "PE",
                    linea: dto.DireccionPostal.Linea ?? string.Empty,
                    ubigeo: dto.DireccionPostal.Ubigeo ?? string.Empty,
                    departamento: dto.DireccionPostal.Departamento ?? string.Empty,
                    provincia: dto.DireccionPostal.Provincia ?? string.Empty,
                    distrito: dto.DireccionPostal.Distrito ?? string.Empty,
                    addressTypeCode: dto.DireccionPostal.AddressTypeCode ?? "0000",
                    urbanizacion: dto.DireccionPostal.Urbanizacion,
                    referencia: dto.DireccionPostal.Referencia
                );
            }
            else
            {
                direccionPostal = DireccionPostal.From(
                    paisCodigoIso: "PE",
                    linea: string.Empty,
                    ubigeo: string.Empty,
                    departamento: string.Empty,
                    provincia: string.Empty,
                    distrito: string.Empty,
                    addressTypeCode: "0000"
                );
            }

            var cliente = new Cliente(
                Guid.NewGuid(),
                docId,
                dto.RazonSocialONombres!,
                dto.Correo,
                dto.Celular,
                direccionPostal,
                Enum.Parse<TipoCliente>(dto.TipoCliente!),
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