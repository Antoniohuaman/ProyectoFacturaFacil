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
        // Usamos clave compuesta: EmpresaId|EstablecimientoId(optional)|CodigoCaja
        private readonly ConcurrentDictionary<string, TurnoCaja> _turnos = new();

        private static string Key(CodigoCaja codigoCaja, SharedKernel.ValueObjects.EmpresaId empresaId, SharedKernel.ValueObjects.EstablecimientoId? establecimientoId)
            => $"{empresaId.Value:D}|{(establecimientoId?.Value.ToString("D") ?? "-")}|{codigoCaja.Value}";

        public Task<TurnoCaja?> GetTurnoAbiertoAsync(CodigoCaja codigoCaja, SharedKernel.ValueObjects.EmpresaId empresaId, SharedKernel.ValueObjects.EstablecimientoId? establecimientoId = null)
        {
            var key = Key(codigoCaja, empresaId, establecimientoId);
            if (_turnos.TryGetValue(key, out var turno) && turno.EstaAbierto)
                return Task.FromResult<TurnoCaja?>(turno);
            return Task.FromResult<TurnoCaja?>(null);
        }

        public Task AddTurnoCajaAsync(TurnoCaja turno)
        {
            var key = Key(turno.CodigoCaja, turno.EmpresaId, turno.EstablecimientoId);
            if (!_turnos.TryAdd(key, turno))
                throw new InvalidOperationException("Turno ya existe");
            return Task.CompletedTask;
        }

        public Task UpdateTurnoCajaAsync(TurnoCaja turno)
        {
            var key = Key(turno.CodigoCaja, turno.EmpresaId, turno.EstablecimientoId);
            _turnos[key] = turno;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<MovimientoCaja>> GetMovimientosAsync(CodigoCaja codigoCaja, SharedKernel.ValueObjects.EmpresaId empresaId, SharedKernel.ValueObjects.EstablecimientoId? establecimientoId = null)
        {
            var key = Key(codigoCaja, empresaId, establecimientoId);
            if (_turnos.TryGetValue(key, out var turno))
                return Task.FromResult((IReadOnlyCollection<MovimientoCaja>)turno.Movimientos);

            return Task.FromResult((IReadOnlyCollection<MovimientoCaja>)Array.Empty<MovimientoCaja>());
        }

        public Task<TurnoCaja?> GetTurnoCerradoAsync(CodigoCaja codigoCaja, SharedKernel.ValueObjects.EmpresaId empresaId, SharedKernel.ValueObjects.EstablecimientoId? establecimientoId = null)
        {
            var key = Key(codigoCaja, empresaId, establecimientoId);
            if (_turnos.TryGetValue(key, out var turno) && turno.EstaCerrado)
                return Task.FromResult<TurnoCaja?>(turno);
            return Task.FromResult<TurnoCaja?>(null);
        }
    }
}
