using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Application.UseCases;

namespace ListaPreciosBC.Application.UseCases
{
    public class ConfigurarListaPrecioUseCase
    {
        private readonly IListaPrecioRepository _repo;
        private readonly IUnitOfWork _uow;

        public ConfigurarListaPrecioUseCase(IListaPrecioRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<Guid> HandleAsync(ConfigurarListaPrecioDto dto)
        {
            // Validar unicidad (RN-LP-001)
            var existente = await _repo.GetVigentePorCriterioAsync(dto.TipoLista, dto.Criterio, Guid.Empty, dto.Vigencia.FechaInicio);
            if (existente != null)
                throw new Exception("Ya existe una lista vigente con el mismo criterio y producto.");

            // Validar vigencia (RN-LP-002)
            if (!dto.Vigencia.EsValido())
                throw new Exception("El periodo de vigencia no es válido.");

            var listaPrecio = new ListaPrecio(
                Guid.NewGuid(),
                dto.TipoLista,
                dto.Criterio,
                dto.Moneda,
                dto.Prioridad,
                dto.Vigencia,
                DateTime.UtcNow
            );

            await _repo.AddAsync(listaPrecio);
            await _uow.CommitAsync();

            // Aquí podrías publicar el evento ListaPrecioCreada si tienes un event bus

            return listaPrecio.ListaPrecioId;
        }
    }
}