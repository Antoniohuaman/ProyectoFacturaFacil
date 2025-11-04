using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface IReservaStockRepository
	{
		Task<ReservaStock?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Guid reservaId, CancellationToken ct = default);
		Task GuardarAsync(ReservaStock reserva, CancellationToken ct = default);
	}
}

