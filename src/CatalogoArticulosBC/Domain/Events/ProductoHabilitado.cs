using SharedKernel.Events;
using System;

namespace CatalogoArticulosBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que indica que un producto ha sido habilitado (reactivado) en el catálogo.
	/// Permite auditar y reaccionar ante la habilitación de productos previamente inhabilitados.
	/// </summary>
	public sealed class ProductoHabilitado : DomainEvent
	{
		/// <summary>Identificador único del producto habilitado.</summary>
		public Guid ProductoId { get; }

		/// <summary>Motivo o razón de la habilitación (opcional).</summary>
		public string? Motivo { get; }

		/// <summary>Usuario que realizó la habilitación.</summary>
		public string Usuario { get; }

		public ProductoHabilitado(Guid productoId, string usuario, string? motivo = null, Guid? eventId = null, DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ProductoId = productoId;
			Usuario = usuario ?? string.Empty;
			Motivo = motivo;
		}
	}
}
