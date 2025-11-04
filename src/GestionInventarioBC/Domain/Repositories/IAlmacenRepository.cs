using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface IAlmacenRepository
	{
		Task<Almacen?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, CancellationToken ct = default);
		Task GuardarAsync(Almacen almacen, CancellationToken ct = default);
	}
}

