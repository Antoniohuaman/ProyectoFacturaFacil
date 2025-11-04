using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Application.Interfaces;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Consultas
{
	/// <summary>
	/// Consulta movimientos por filtros básicos (rango de fechas, SKU, tipo, motivo) en un almacén.
	/// </summary>
	public sealed class ConsultarMovimientosUseCase
	{
		public readonly record struct Request(
			Guid EstablecimientoId,
			Guid AlmacenId,
			DateTimeOffset? Desde,
			DateTimeOffset? Hasta,
			string? Sku,
			string? Tipo, // Ingreso/Egreso/AjustePositivo/AjusteNegativo/TransferenciaEntrada/TransferenciaSalida
			string? Motivo // enum MotivoMovimiento
		);

		public readonly record struct Item(
			Guid MovimientoId,
			DateTimeOffset Fecha,
			string Tipo,
			string Motivo,
			IReadOnlyList<(string Sku, decimal Cantidad)> Lineas
		);

		public readonly record struct Response(IReadOnlyList<Item> Movimientos);

		private readonly IMovimientoInventarioRepository _repo;
		private readonly ITenantContext _tenant;
        private readonly ICatalogoReadModel _catalogo;

		public ConsultarMovimientosUseCase(IMovimientoInventarioRepository repo, ITenantContext tenant, ICatalogoReadModel catalogo)
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

			ProductoId? productoId = null;
			if (!string.IsNullOrWhiteSpace(req.Sku))
			{
				productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, req.Sku.Trim(), ct);
			}
			TipoMovimiento? tipo = null;
			if (!string.IsNullOrWhiteSpace(req.Tipo) && Enum.TryParse<TipoMovimiento>(req.Tipo, true, out var t)) tipo = t;
			MotivoMovimiento? motivo = null;
			if (!string.IsNullOrWhiteSpace(req.Motivo) && Enum.TryParse<MotivoMovimiento>(req.Motivo, true, out var m)) motivo = m;

			var lista = await _repo.ListarAsync(empresaId, estId, almId, req.Desde, req.Hasta, productoId, tipo, motivo, ct);
			var items = lista.Select(m => new Item(
				MovimientoId: m.MovimientoId,
				Fecha: m.Fecha,
				Tipo: m.Tipo.ToString(),
				Motivo: m.Motivo.ToString(),
				Lineas: m.Lineas.Select(l => (l.ProductoId.Value.ToString(), l.Cantidad.Value)).ToList()
			)).ToList();

			return new Response(items);
		}
	}
}

