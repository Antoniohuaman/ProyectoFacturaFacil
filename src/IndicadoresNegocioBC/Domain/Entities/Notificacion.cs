using System;
using SharedKernel.ValueObjects;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Domain.Entities
{
	/// <summary>
	/// Entidad simple que representa una notificación configurada para un indicador de negocio.
	/// No contiene lógica de negocio, solo propiedades para persistencia o mapeo.
	/// </summary>
	public class Notificacion
	{
		public Guid Id { get; set; }
		public Guid IndicadorId { get; set; }
	    public required EmpresaId EmpresaId { get; set; }
	    public required EstablecimientoId EstablecimientoId { get; set; }
	    public required UsuarioId UsuarioId { get; set; }
		public string Medio { get; set; } = string.Empty; // Ej: Email, SMS, etc.
		public string Destinatario { get; set; } = string.Empty; // Email, teléfono, etc.
		public string Horario { get; set; } = string.Empty; // Ej: "08:00-18:00"
		public bool Activo { get; set; }
		public DateTime FechaCreacion { get; set; }
		public DateTime? FechaUltimaModificacion { get; set; }
	}
}
