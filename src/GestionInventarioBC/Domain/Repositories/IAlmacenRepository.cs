using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
using System.Collections.Generic;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface IAlmacenRepository
	{
		Task<Almacen?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, CancellationToken ct = default);
		Task<IReadOnlyList<Almacen>> ListarAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, CancellationToken ct = default);
		Task GuardarAsync(Almacen almacen, CancellationToken ct = default);
	}
}

