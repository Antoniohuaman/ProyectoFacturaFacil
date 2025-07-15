using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Application.UseCases
{
    public class RegistrarOperacionClienteUseCase
    {
        private readonly IGestionClientesRepository _repo;

        public RegistrarOperacionClienteUseCase(IGestionClientesRepository repo)
        {
            _repo = repo;
        }

        public async Task HandleAsync(RegistrarOperacionClienteDto dto)
        {
            var operacion = new OperacionCliente(
                Guid.NewGuid(),
                dto.TipoOperacion,
                new MontoOperacion(dto.Monto),
                new ReferenciaId(dto.Referencia),
                dto.FechaOperacion
            );

            await _repo.RegistrarOperacionClienteAsync(dto.ClienteId, operacion);

            // Aquí podrías publicar el evento OperacionClienteRegistrada si lo necesitas
        }
    }
}