using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface IStockPorAlmacenRepository
	{
		Task<StockPorAlmacen?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Sku sku, CancellationToken ct = default);
		Task GuardarAsync(StockPorAlmacen stock, CancellationToken ct = default);
	}
}

