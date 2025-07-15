using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestionClientesBC.Application.DTOs;
using GestionClientesBC.Application.Interfaces;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Application.UseCases
{
    public class ConsultarHistorialOperacionesUseCase
    {
        private readonly IGestionClientesRepository _repo;

        public ConsultarHistorialOperacionesUseCase(IGestionClientesRepository repo)
        {
            _repo = repo;
        }

        public async Task<ICollection<OperacionClienteDto>> HandleAsync(ConsultarHistorialOperacionesDto dto)
        {
            var operaciones = await _repo.ObtenerOperacionesPorClienteIdAsync(
                dto.ClienteId,
                dto.FechaDesde,
                dto.FechaHasta,
                dto.TipoOperacion,
                dto.Page,
                dto.PageSize
            );

            return operaciones.Select(OperacionClienteDto.FromEntity).ToList();
        }
    }
}