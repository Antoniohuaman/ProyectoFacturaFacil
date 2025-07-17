using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;

namespace ListaPreciosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryListaPrecioRepository : IListaPrecioRepository
    {
        private readonly List<ListaPrecio> _listas = new();
        private readonly List<HistorialPrecio> _historiales = new();

        public Task<ListaPrecio?> GetByIdAsync(Guid listaPrecioId)
            => Task.FromResult(_listas.FirstOrDefault(x => x.ListaPrecioId == listaPrecioId));

        public Task<ListaPrecio?> GetVigentePorCriterioAsync(TipoLista tipoLista, CriterioLista criterio, Guid productoId, DateTime fecha)
        {
            var lista = _listas.FirstOrDefault(x =>
                x.TipoLista == tipoLista &&
                x.Criterio.Equals(criterio) &&
                x.Activa &&
                x.Vigencia.FechaInicio <= fecha &&
                x.Vigencia.FechaFin >= fecha
            );
            return Task.FromResult(lista);
        }

        public Task AddAsync(ListaPrecio listaPrecio)
        {
            _listas.Add(listaPrecio);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ListaPrecio listaPrecio)
        {
            // Para InMemory, nada que hacer (referencia ya actualizada)
            return Task.CompletedTask;
        }

        // Métodos agregados para cumplir la interfaz

        public Task<PrecioEspecifico?> ObtenerPrecioEspecificoPorIdAsync(Guid precioEspecificoId)
        {
            var precio = _listas
                .SelectMany(l => l.Precios)
                .FirstOrDefault(p => p.PrecioEspecificoId == precioEspecificoId);
            return Task.FromResult(precio);
        }

        public Task AgregarHistorialAsync(HistorialPrecio historial)
        {
            _historiales.Add(historial);
            return Task.CompletedTask;
        }

        public Task ActualizarPrecioEspecificoAsync(PrecioEspecifico precio)
        {
            // En memoria, nada que hacer si ya tienes la referencia actualizada.
            return Task.CompletedTask;
        }
    }
}