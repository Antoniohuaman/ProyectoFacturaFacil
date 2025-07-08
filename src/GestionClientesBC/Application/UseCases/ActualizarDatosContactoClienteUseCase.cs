using System;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;

namespace GestionClientesBC.Application.UseCases
{
    public class ActualizarDatosContactoClienteUseCase
    {
        private readonly IGestionClientesRepository _repo;
        private readonly IUnitOfWork _uow;

        public ActualizarDatosContactoClienteUseCase(IGestionClientesRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task HandleAsync(ActualizarDatosContactoClienteDto dto)
    {
    var cliente = await _repo.GetByIdAsync(dto.ClienteId)
        ?? throw new Exception("Cliente no encontrado.");

    cliente.ActualizarDatosContacto(dto.NuevoCorreo, dto.NuevoCelular);

    await _repo.UpdateAsync(cliente);
    await _uow.CommitAsync();
    }
    }
}