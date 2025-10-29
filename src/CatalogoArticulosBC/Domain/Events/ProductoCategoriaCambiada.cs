using SharedKernel.Events;
using System;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que indica que la categoría de un producto ha cambiado.
	/// Permite auditar y reaccionar ante cambios de clasificación.
	/// </summary>
	public sealed class ProductoCategoriaCambiada : DomainEvent
	{
		/// <summary>Identificador único del producto.</summary>
		public Guid ProductoId { get; }

		/// <summary>Empresa (tenant) del producto.</summary>
		public EmpresaId EmpresaId { get; }

		/// <summary>Categoría anterior del producto.</summary>
		public string CategoriaAnterior { get; }

		/// <summary>Nueva categoría asignada al producto.</summary>
		public string CategoriaNueva { get; }

		/// <summary>Usuario que realizó el cambio.</summary>
		public string Usuario { get; }

		public ProductoCategoriaCambiada(Guid productoId, EmpresaId empresaId, string categoriaAnterior, string categoriaNueva, string usuario, Guid? eventId = null, DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ProductoId = productoId;
			EmpresaId = empresaId;
			CategoriaAnterior = categoriaAnterior ?? string.Empty;
			CategoriaNueva = categoriaNueva ?? string.Empty;
			Usuario = usuario ?? string.Empty;
		}
	}
}
