using System;
using SharedKernel.Events;

namespace CatalogoArticulosBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que indica que un producto ha sido eliminado del catálogo.
	/// </summary>
	
	public sealed class ProductoEliminado : IDomainEvent
	{
		/// <summary>Identificador único del producto eliminado (SKU).</summary>
		public string Sku { get; }

		/// <summary>Motivo de la eliminación (opcional).</summary>
		public string? Motivo { get; }

		/// <summary>Usuario que realizó la eliminación.</summary>
		public string Usuario { get; }

		/// <summary>Fecha y hora de la eliminación.</summary>
		public DateTime EliminadoEn { get; }

		public Guid EventId { get; }
		public DateTime OccurredOn { get; }

		public ProductoEliminado(string sku, string usuario, DateTime eliminadoEn, string? motivo = null)
		{
			Sku = sku ?? throw new ArgumentNullException(nameof(sku));
			Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
			EliminadoEn = eliminadoEn;
			Motivo = motivo;
			EventId = Guid.NewGuid();
			OccurredOn = DateTime.UtcNow;
		}
	}
}
