using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface IMovimientoInventarioRepository
	{
		Task<MovimientoInventario?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Guid movimientoId, CancellationToken ct = default);
		Task GuardarAsync(MovimientoInventario movimiento, CancellationToken ct = default);
	}
}

