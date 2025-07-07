using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;
using ControlCajaBC.Domain.Aggregates;
using ControlCajaBC.Domain.Entities;
using ControlCajaBC.Domain.ValueObjects;

namespace ControlCajaBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryControlCajaRepository : IControlCajaRepository
    {
        // 1) Diccionario con clave CodigoCaja
        private readonly ConcurrentDictionary<CodigoCaja, TurnoCaja> _turnos
            = new();

        public Task<TurnoCaja?> GetTurnoAbiertoAsync(CodigoCaja codigoCaja)
        {
            if (_turnos.TryGetValue(codigoCaja, out var turno) && turno.EstaAbierto)
                return Task.FromResult<TurnoCaja?>(turno);
            return Task.FromResult<TurnoCaja?>(null);
        }

        public Task AddTurnoCajaAsync(TurnoCaja turno)
        {
            // 2) Usar turno.CodigoCaja
            if (!_turnos.TryAdd(turno.CodigoCaja, turno))
                throw new InvalidOperationException("Turno ya existe");
            return Task.CompletedTask;
        }

        public Task UpdateTurnoCajaAsync(TurnoCaja turno)
        {
            // 2) Usar turno.CodigoCaja
            _turnos[turno.CodigoCaja] = turno;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<MovimientoCaja>> GetMovimientosAsync(CodigoCaja codigoCaja)
        {
            if (_turnos.TryGetValue(codigoCaja, out var turno))
                return Task.FromResult((IReadOnlyCollection<MovimientoCaja>)turno.Movimientos);

            return Task.FromResult((IReadOnlyCollection<MovimientoCaja>)Array.Empty<MovimientoCaja>());
        }

        public Task<TurnoCaja?> GetTurnoCerradoAsync(CodigoCaja codigoCaja)
        {
            if (_turnos.TryGetValue(codigoCaja, out var turno) && turno.EstaCerrado)
                return Task.FromResult<TurnoCaja?>(turno);
            return Task.FromResult<TurnoCaja?>(null);
        }
    }
}
