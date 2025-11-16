using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
	/// <summary>
	/// Evento que describe el cambio de domicilio fiscal del cliente.
	/// </summary>
	public sealed class DireccionClienteActualizada : DomainEvent
	{
		public Guid ClienteId { get; }
		public EmpresaId EmpresaId { get; }
		public string PaisCodigoIso { get; }
		public string? DireccionLinea { get; }
		public string? Ubigeo { get; }
		public string? Departamento { get; }
		public string? Provincia { get; }
		public string? Distrito { get; }
		public string? AddressTypeCode { get; }
		public DateTime FechaActualizacionUtc { get; }

		public DireccionClienteActualizada(
			Guid clienteId,
			EmpresaId empresaId,
			string paisCodigoIso,
			string? direccionLinea,
			string? ubigeo,
			string? departamento,
			string? provincia,
			string? distrito,
			string? addressTypeCode,
			DateTime fechaActualizacionUtc,
			Guid? eventId = null,
			DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ClienteId = clienteId;
			EmpresaId = empresaId;
			PaisCodigoIso = paisCodigoIso;
			DireccionLinea = direccionLinea;
			Ubigeo = ubigeo;
			Departamento = departamento;
			Provincia = provincia;
			Distrito = distrito;
			AddressTypeCode = addressTypeCode;
			FechaActualizacionUtc = fechaActualizacionUtc.Kind == DateTimeKind.Utc
				? fechaActualizacionUtc
				: fechaActualizacionUtc.ToUniversalTime();
		}
	}
}
