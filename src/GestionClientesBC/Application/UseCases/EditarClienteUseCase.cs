using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Application.UseCases
{
    public class EditarClienteUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public EditarClienteUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task EditarAsync(EditarClienteDto dto, IUserContext userContext)
        {
            // 1. Verificar permisos
            if (!userContext.HasPermission("EditarCliente"))
                throw new UnauthorizedAccessException("No tiene permisos para editar clientes.");

            // 2. Buscar cliente
            var cliente = await _repo.GetByIdAsync(dto.ClienteId);
            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            var camposModificados = new Dictionary<string, (object? anterior, object? nuevo)>();

            // 3. Validar y aplicar cambios
            bool contactoModificado = false;
            string nuevoCorreo = cliente.Correo;
            string nuevoCelular = cliente.Celular;

            if (!string.IsNullOrWhiteSpace(dto.Correo) && dto.Correo != cliente.Correo)
            {
                if (!CorreoValido(dto.Correo))
                    throw new ArgumentException("El correo electrónico no tiene formato válido.");
                camposModificados["Correo"] = (cliente.Correo, dto.Correo);
                nuevoCorreo = dto.Correo;
                contactoModificado = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Celular) && dto.Celular != cliente.Celular)
            {
                camposModificados["Celular"] = (cliente.Celular, dto.Celular);
                nuevoCelular = dto.Celular;
                contactoModificado = true;
            }

            if (contactoModificado)
                cliente.ActualizarDatosContacto(nuevoCorreo, nuevoCelular);

            if (!string.IsNullOrWhiteSpace(dto.DireccionPostal) && dto.DireccionPostal != cliente.DireccionPostal)
            {
                camposModificados["DireccionPostal"] = (cliente.DireccionPostal, dto.DireccionPostal);
                cliente.ActualizarDireccion(dto.DireccionPostal);
            }

            if (!string.IsNullOrWhiteSpace(dto.RazonSocialONombres) && dto.RazonSocialONombres != cliente.RazonSocialONombres)
            {
                camposModificados["RazonSocialONombres"] = (cliente.RazonSocialONombres, dto.RazonSocialONombres);
                cliente.ActualizarNombre(dto.RazonSocialONombres);
            }

            if (!string.IsNullOrWhiteSpace(dto.TipoCliente) && Enum.TryParse<TipoCliente>(dto.TipoCliente, out var nuevoTipo) && nuevoTipo != cliente.TipoCliente)
            {
                camposModificados["TipoCliente"] = (cliente.TipoCliente, nuevoTipo);
                cliente.ActualizarTipoCliente(nuevoTipo);
            }

            // Cambio de documento (opcional y poco común)
            if (!string.IsNullOrWhiteSpace(dto.TipoDocumento) && !string.IsNullOrWhiteSpace(dto.NumeroDocumento))
            {
                var nuevoDoc = new DocumentoIdentidad(Enum.Parse<TipoDocumento>(dto.TipoDocumento), dto.NumeroDocumento);
                if (!nuevoDoc.Equals(cliente.DocumentoIdentidad))
                {
                    // Validar unicidad
                    var existente = await _repo.GetByDocumentoIdentidadAsync(nuevoDoc);
                    if (existente != null && existente.ClienteId != cliente.ClienteId)
                        throw new InvalidOperationException("Documento ya registrado en otro cliente.");
                    camposModificados["DocumentoIdentidad"] = (cliente.DocumentoIdentidad, nuevoDoc);
                    cliente.ActualizarDocumentoIdentidad(nuevoDoc);
                }
            }

            if (camposModificados.Count == 0)
                return; // O lanzar excepción "No se realizaron cambios"

            // Registrar evento de modificación (esto también puede actualizar la fecha internamente)
            cliente.RegistrarModificacion(camposModificados);

            await _repo.UpdateAsync(cliente);
            await _uow.CommitAsync();

            // Aquí podrías publicar el evento ClienteModificado si tienes un bus de eventos
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