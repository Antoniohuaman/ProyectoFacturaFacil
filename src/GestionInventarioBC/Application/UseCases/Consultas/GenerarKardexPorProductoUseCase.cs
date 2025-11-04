using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;
using GestionInventarioBC.Application.Interfaces;

namespace GestionInventarioBC.Application.UseCases.Consultas
{
	/// <summary>
	/// Genera Kardex simple por producto (SKU) en un almacén para un rango de fechas.
	/// </summary>
	public sealed class GenerarKardexPorProductoUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, string Sku, DateTimeOffset? Desde, DateTimeOffset? Hasta);

		public readonly record struct Item(
			DateTimeOffset Fecha,
			string Tipo,
			decimal Entrada,
			decimal Salida,
			decimal SaldoAcumulado
		);

		public readonly record struct Response(IReadOnlyList<Item> Movimientos);

	private readonly IMovimientoInventarioRepository _repo;
	private readonly ITenantContext _tenant;
	private readonly ICatalogoReadModel _catalogo;

		public GenerarKardexPorProductoUseCase(IMovimientoInventarioRepository repo, ITenantContext tenant, ICatalogoReadModel catalogo)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);
			var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, req.Sku, ct)
				?? throw new InvalidOperationException("No existe producto para el SKU indicado.");

			var lista = await _repo.ListarAsync(empresaId, estId, almId, req.Desde, req.Hasta, productoId, null, null, ct);
			var ordenado = lista.OrderBy(m => m.Fecha).ToList();
			decimal saldo = 0m;
			var items = new List<Item>(ordenado.Count);
			foreach (var m in ordenado)
			{
				var linea = m.Lineas.FirstOrDefault(l => l.ProductoId.Equals(productoId));
				if (linea is null) continue;

				var entrada = 0m;
				var salida = 0m;
				switch (m.Tipo)
				{
					case TipoMovimiento.Ingreso:
					case TipoMovimiento.AjustePositivo:
					case TipoMovimiento.TransferenciaEntrada:
						entrada = linea.Cantidad.Value;
						saldo += entrada;
						break;
					case TipoMovimiento.Egreso:
					case TipoMovimiento.AjusteNegativo:
					case TipoMovimiento.TransferenciaSalida:
						salida = linea.Cantidad.Value;
						saldo -= salida;
						break;
				}
				items.Add(new Item(m.Fecha, m.Tipo.ToString(), entrada, salida, saldo));
			}

			return new Response(items);
		}
	}
}

