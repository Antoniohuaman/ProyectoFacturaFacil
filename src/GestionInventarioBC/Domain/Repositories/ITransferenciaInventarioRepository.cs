using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Repositories
{
	public interface ITransferenciaInventarioRepository
	{
		Task<TransferenciaInventario?> ObtenerAsync(EmpresaId empresaId, Guid transferenciaId, CancellationToken ct = default);
		Task GuardarAsync(TransferenciaInventario transferencia, CancellationToken ct = default);
	}
}

