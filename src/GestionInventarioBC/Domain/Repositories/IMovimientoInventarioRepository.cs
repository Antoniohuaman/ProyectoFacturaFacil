using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface IMovimientoInventarioRepository
	{
		Task<MovimientoInventario?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Guid movimientoId, CancellationToken ct = default);
		Task<IReadOnlyList<MovimientoInventario>> ListarAsync(
			EmpresaId empresaId,
			EstablecimientoId establecimientoId,
			AlmacenId almacenId,
			DateTimeOffset? desde = null,
			DateTimeOffset? hasta = null,
			Sku? sku = null,
			GestionInventarioBC.Domain.ValueObjects.TipoMovimiento? tipo = null,
			GestionInventarioBC.Domain.ValueObjects.MotivoMovimiento? motivo = null,
			CancellationToken ct = default);
		Task GuardarAsync(MovimientoInventario movimiento, CancellationToken ct = default);
	}
}

