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
			string? Motivo, // enum MotivoMovimiento
			int? Page = null,
			int? PageSize = null
		);

		public readonly record struct Linea(string Sku, string Nombre, decimal Cantidad);

		public readonly record struct Item(
			Guid MovimientoId,
			DateTimeOffset Fecha,
			string Tipo,
			string Motivo,
			IReadOnlyList<Linea> Lineas
		);

		public readonly record struct Response(int Total, IReadOnlyList<Item> Items);

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

			// Enriquecer líneas con SKU/Nombre
			var enriched = new List<Item>(lista.Count);
			foreach (var m in lista)
			{
				var lineas = new List<Linea>(m.Lineas.Count);
				foreach (var l in m.Lineas)
				{
					var present = await _catalogo.TryGetSkuYNombreAsync(empresaId, l.ProductoId, ct);
					lineas.Add(new Linea(
						Sku: present?.Sku ?? string.Empty,
						Nombre: present?.Nombre ?? string.Empty,
						Cantidad: l.Cantidad.Value
					));
				}
				enriched.Add(new Item(
					MovimientoId: m.MovimientoId,
					Fecha: m.Fecha,
					Tipo: m.Tipo.ToString(),
					Motivo: m.Motivo.ToString(),
					Lineas: lineas
				));
			}

			// Paginación in-memory (TODO: mover a repo si procede)
			var total = enriched.Count;
			var page = req.Page.GetValueOrDefault(1);
			var pageSize = req.PageSize.GetValueOrDefault(50);
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = 50;
			var items = enriched.Skip((page - 1) * pageSize).Take(pageSize).ToList();

			return new Response(total, items);
		}
	}
}

