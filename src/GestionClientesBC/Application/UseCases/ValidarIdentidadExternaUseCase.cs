using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.Exceptions;

namespace GestionClientesBC.Application.UseCases
{
    public class ValidarIdentidadExternaUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IServicioIdentidadExterna _servicio;
        private readonly IEventBus _eventBus;

        public ValidarIdentidadExternaUseCase(
            IGestionClientesRepository repo,
            IServicioIdentidadExterna servicio,
            IEventBus eventBus)
        {
            _repo = repo;
            _servicio = servicio;
            _eventBus = eventBus;
        }

        public async Task ValidarAsync(ValidarIdentidadExternaDto dto)
        {
            var cliente = await _repo.GetByIdAsync(dto.ClienteId)
                ?? throw new Exception("Cliente no encontrado");

            var doc = cliente.DocumentoIdentidad;
            DatosIdentidadExterna? datos = null;

            if (doc.Tipo == TipoDocumento.DNI)
                datos = await _servicio.ConsultarPorDniAsync(doc.Numero);
            else if (doc.Tipo == TipoDocumento.RUC)
                datos = await _servicio.ConsultarPorRucAsync(doc.Numero);
            else
                throw new Exception("Tipo de documento no soportado");

            if (datos == null)
                throw new DocumentoNoEncontradoException(doc.Numero);

            bool modificado = false;

            if (doc.Tipo == TipoDocumento.DNI && !string.IsNullOrWhiteSpace(datos.NombreCompleto))
            {
                if (cliente.RazonSocialONombres != datos.NombreCompleto)
                {
                    cliente.ActualizarNombre(datos.NombreCompleto);
                    modificado = true;
                }
            }
            else if (doc.Tipo == TipoDocumento.RUC)
            {
                if (!string.IsNullOrWhiteSpace(datos.RazonSocial) &&
                    (string.IsNullOrWhiteSpace(cliente.RazonSocialONombres) || cliente.RazonSocialONombres == "GENÉRICO"))
                {
                    cliente.ActualizarNombre(datos.RazonSocial);
                    modificado = true;
                }
                if (!string.IsNullOrWhiteSpace(datos.DireccionFiscal) && string.IsNullOrWhiteSpace(cliente.DireccionPostal))
                {
                    cliente.ActualizarDireccion(datos.DireccionFiscal);
                    modificado = true;
                }
            }

            if (modificado)
            {
                await _repo.UpdateAsync(cliente);
                await _eventBus.PublishAsync(new ClienteModificado(cliente.ClienteId, DateTime.UtcNow));
            }
        }
    }
}