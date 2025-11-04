using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
using System.Collections.Generic;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface IStockPorAlmacenRepository
	{
		Task<StockPorAlmacen?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, ProductoId productoId, CancellationToken ct = default);
		Task<IReadOnlyList<StockPorAlmacen>> ListarPorAlmacenAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, CancellationToken ct = default);
		Task GuardarAsync(StockPorAlmacen stock, CancellationToken ct = default);
	}
}

