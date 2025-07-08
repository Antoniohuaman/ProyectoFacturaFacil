using System;
using System.Threading.Tasks;
using ListaPreciosBC.Application.DTOs;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Entities;
using ListaPreciosBC.Domain.Aggregates;

namespace ListaPreciosBC.Application.UseCases
{
    public class AgregarPrecioEspecificoUseCase
    {
        private readonly IListaPrecioRepository _repo;
        private readonly IUnitOfWork _uow;

        public AgregarPrecioEspecificoUseCase(IListaPrecioRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<Guid> HandleAsync(AgregarPrecioEspecificoDto dto)
        {
            var lista = await _repo.GetByIdAsync(dto.ListaPrecioId)
                ?? throw new Exception("Lista de precios no encontrada.");

            var precio = new PrecioEspecifico(
                Guid.NewGuid(),
                dto.Valor,
                dto.Moneda,
                dto.Prioridad,
                dto.Vigencia,
                dto.RangoVolumen
            );

            // Aquí podrías agregar validaciones de negocio adicionales

            // Suponiendo que ListaPrecio tiene un método para agregar precios:
            // lista.AgregarPrecioEspecifico(precio);

            // Por ahora, accedemos a la colección interna (ajusta según tu modelo)
            lista.AgregarPrecioEspecifico(precio);

            await _repo.UpdateAsync(lista);
            await _uow.CommitAsync();

            return precio.PrecioEspecificoId;
        }
    }
}