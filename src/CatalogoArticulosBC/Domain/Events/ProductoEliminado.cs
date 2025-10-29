using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que indica que un producto ha sido eliminado del catálogo.
	/// </summary>
	
	public sealed class ProductoEliminado : DomainEvent
	{
		/// <summary>Identificador único del producto eliminado (SKU).</summary>
		public string Sku { get; }

		/// <summary>Empresa (tenant) del producto eliminado.</summary>
		public EmpresaId EmpresaId { get; }

		/// <summary>Motivo de la eliminación (opcional).</summary>
		public string? Motivo { get; }

		/// <summary>Usuario que realizó la eliminación.</summary>
		public string Usuario { get; }

		public ProductoEliminado(string sku, EmpresaId empresaId, string usuario, string? motivo = null, Guid? eventId = null, DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			Sku = sku ?? throw new ArgumentNullException(nameof(sku));
			EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
			Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
			Motivo = motivo;
		}
	}
}
