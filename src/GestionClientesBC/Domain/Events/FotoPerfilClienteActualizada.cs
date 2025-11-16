using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
	/// <summary>
	/// Evento que registra la creación o eliminación de la foto de perfil del cliente.
	/// </summary>
	public sealed class FotoPerfilClienteActualizada : DomainEvent
	{
		public Guid ClienteId { get; }
		public EmpresaId EmpresaId { get; }
		public bool TieneFoto { get; }
		public string? NombreArchivo { get; }
		public string? UrlPublica { get; }
		public DateTime FechaActualizacionUtc { get; }

		public FotoPerfilClienteActualizada(
			Guid clienteId,
			EmpresaId empresaId,
			bool tieneFoto,
			string? nombreArchivo,
			string? urlPublica,
			DateTime fechaActualizacionUtc,
			Guid? eventId = null,
			DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ClienteId = clienteId;
			EmpresaId = empresaId;
			TieneFoto = tieneFoto;
			NombreArchivo = nombreArchivo;
			UrlPublica = urlPublica;
			FechaActualizacionUtc = fechaActualizacionUtc.Kind == DateTimeKind.Utc
				? fechaActualizacionUtc
				: fechaActualizacionUtc.ToUniversalTime();
		}
	}
}
